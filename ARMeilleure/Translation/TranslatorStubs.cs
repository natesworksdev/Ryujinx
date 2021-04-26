using ARMeilleure.Common;
using ARMeilleure.Instructions;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation.Cache;
using System;
using System.Runtime.InteropServices;

using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Translation
{
    class TranslatorStubs
    {
        public IntPtr DispatchStub { get; }

        public TranslatorStubs(Translator translator)
        {
            DispatchStub = GenerateDispatchStub(translator);
        }

        private static IntPtr GenerateDispatchStub(Translator translator)
        {
            var stubOffsetIndex = translator.CountTable.Allocate();

            var context = new EmitterContext();

            Operand lblFallback = Label();
            Operand lblEnd = Label();

            // Load the target guest address from the native context.
            Operand nativeContext = context.LoadArgument(OperandType.I64, 0);
            Operand guestAddress = context.Load(OperandType.I64,
                context.Add(nativeContext, Const((ulong)NativeContext.GetDispatchAddressOffset())));

            // Check if guest address is within range of the AddressTable.
            Operand masked = context.BitwiseAnd(guestAddress, Const(~translator.FunctionTable.Mask));
            context.BranchIfTrue(lblFallback, masked);

            Operand index = null;
            Operand page = Const((long)translator.FunctionTable.Base);

            for (int i = 0; i < translator.FunctionTable.Levels.Length; i++)
            {
                ref var level = ref translator.FunctionTable.Levels[i];

                // level.Mask is not used directly because it is more often bigger than 32-bits, so it will not
                // be encoded as an immediate on x86's bitwise and operation.
                Operand mask = Const(level.Mask >> level.Index);

                index = context.BitwiseAnd(context.ShiftRightUI(guestAddress, Const(level.Index)), mask);

                if (i < translator.FunctionTable.Levels.Length - 1)
                {
                    page = context.Load(OperandType.I64, context.Add(page, context.ShiftLeft(index, Const(3))));

                    context.BranchIfFalse(lblFallback, page);
                }
            }

            Operand hostAddress;

            Operand offsetAddr = context.Add(page, context.ShiftLeft(index, Const(2)));
            Operand offset = context.Load(OperandType.I32, offsetAddr);
            Operand stubOffset = context.Load(OperandType.I32, Const(ref translator.CountTable.GetValue(stubOffsetIndex)));

            // Since the FunctionTable will be filled with the offset to this stub, we have to determine when this is
            // the case from this stub itself, otherwise this will tailcall into an infinite loop. We do this by
            // allocating a slot in the CountTable and storing the offset of the stub there once its mapped on the code
            // cache.
            context.BranchIf(lblFallback, offset, stubOffset, Comparison.Equal);

            hostAddress = context.Add(Const((long)JitCache.Base), context.ZeroExtend32(OperandType.I64, offset));
            context.Tailcall(hostAddress, nativeContext);

            context.MarkLabel(lblFallback);
            hostAddress = context.Call(typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetFunctionAddress)), guestAddress);
            context.Tailcall(hostAddress, nativeContext);

            var cfg = context.GetControlFlowGraph();
            var retType = OperandType.I64;
            var argTypes = new[] { OperandType.I64 };

            var func = Compiler.Compile<GuestFunction>(cfg, argTypes, retType, CompilerOptions.HighCq);

            var pointer = Marshal.GetFunctionPointerForDelegate(func);

            // Store offset of the stub into the slot.
            translator.CountTable.GetValue(stubOffsetIndex) = JitCache.Offset(pointer);

            return pointer;
        }
    }
}
