using ARMeilleure.Common;
using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;
using ARMeilleure.Translation.Cache;
using ARMeilleure.Translation.PTC;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static class InstEmitFlowHelper
    {
        public static void EmitCondBranch(ArmEmitterContext context, Operand target, Condition cond)
        {
            if (cond != Condition.Al)
            {
                context.BranchIfTrue(target, GetCondTrue(context, cond));
            }
            else
            {
                context.Branch(target);
            }
        }

        public static Operand GetCondTrue(ArmEmitterContext context, Condition condition)
        {
            Operand cmpResult = context.TryGetComparisonResult(condition);

            if (cmpResult != null)
            {
                return cmpResult;
            }

            Operand value = Const(1);

            Operand Inverse(Operand val)
            {
                return context.BitwiseExclusiveOr(val, Const(1));
            }

            switch (condition)
            {
                case Condition.Eq:
                    value = GetFlag(PState.ZFlag);
                    break;

                case Condition.Ne:
                    value = Inverse(GetFlag(PState.ZFlag));
                    break;

                case Condition.GeUn:
                    value = GetFlag(PState.CFlag);
                    break;

                case Condition.LtUn:
                    value = Inverse(GetFlag(PState.CFlag));
                    break;

                case Condition.Mi:
                    value = GetFlag(PState.NFlag);
                    break;

                case Condition.Pl:
                    value = Inverse(GetFlag(PState.NFlag));
                    break;

                case Condition.Vs:
                    value = GetFlag(PState.VFlag);
                    break;

                case Condition.Vc:
                    value = Inverse(GetFlag(PState.VFlag));
                    break;

                case Condition.GtUn:
                {
                    Operand c = GetFlag(PState.CFlag);
                    Operand z = GetFlag(PState.ZFlag);

                    value = context.BitwiseAnd(c, Inverse(z));

                    break;
                }

                case Condition.LeUn:
                {
                    Operand c = GetFlag(PState.CFlag);
                    Operand z = GetFlag(PState.ZFlag);

                    value = context.BitwiseOr(Inverse(c), z);

                    break;
                }

                case Condition.Ge:
                {
                    Operand n = GetFlag(PState.NFlag);
                    Operand v = GetFlag(PState.VFlag);

                    value = context.ICompareEqual(n, v);

                    break;
                }

                case Condition.Lt:
                {
                    Operand n = GetFlag(PState.NFlag);
                    Operand v = GetFlag(PState.VFlag);

                    value = context.ICompareNotEqual(n, v);

                    break;
                }

                case Condition.Gt:
                {
                    Operand n = GetFlag(PState.NFlag);
                    Operand z = GetFlag(PState.ZFlag);
                    Operand v = GetFlag(PState.VFlag);

                    value = context.BitwiseAnd(Inverse(z), context.ICompareEqual(n, v));

                    break;
                }

                case Condition.Le:
                {
                    Operand n = GetFlag(PState.NFlag);
                    Operand z = GetFlag(PState.ZFlag);
                    Operand v = GetFlag(PState.VFlag);

                    value = context.BitwiseOr(z, context.ICompareNotEqual(n, v));

                    break;
                }
            }

            return value;
        }

        public static void EmitCall(ArmEmitterContext context, ulong immediate)
        {
            bool isRecursive = immediate == context.EntryAddress;

            if (isRecursive)
            {
                context.Branch(context.GetLabel(immediate));
            }
            else
            {
                EmitTableBranch(context, Const(immediate), isJump: false);
            }
        }

        public static void EmitVirtualCall(ArmEmitterContext context, Operand target)
        {
            EmitTableBranch(context, target, isJump: false);
        }

        public static void EmitVirtualJump(ArmEmitterContext context, Operand target, bool isReturn)
        {
            if (isReturn)
            {
                context.Return(target);
            }
            else
            {
                EmitTableBranch(context, target, isJump: true);
            }
        }

        public static void EmitTailContinue(ArmEmitterContext context, Operand address)
        {
            // Left option here as it may be useful if we need to return to managed rather than tail call in future.
            // (eg. for debug)
            bool useTailContinue = true;

            if (useTailContinue)
            {
                EmitTableBranch(context, address, isJump: true);
            }
            else
            {
                context.Return(address);
            }
        }

        private static void EmitTableBranch(ArmEmitterContext c, Operand guestAddress, bool isJump)
        {
            c.StoreToContext();

            if (guestAddress.Type == OperandType.I32)
            {
                guestAddress = c.ZeroExtend32(OperandType.I64, guestAddress);
            }

            Operand hostAddress;
            Operand index0 = c.BitwiseAnd(c.ShiftRightUI(guestAddress, Const(2)), Const(0x7FFFFul));
            Operand offsetAddr;

            Operand lblFallback = Label();
            Operand lblEnd = Label();

            // If address is mapped onto the function table, we can do an inlined table walk. Otherwise we fallback
            // onto the translator.
            if (c.FunctionTable.IsMapped(guestAddress.Value))
            {
                // If the guest address specified is a constant, we can skip the table walk.
                if (guestAddress.Kind == OperandKind.Constant)
                {
                    offsetAddr = Const(ref c.FunctionTable.GetValue(guestAddress.Value));
                }
                else
                {
                    Operand masked = c.BitwiseAnd(guestAddress, Const(~AddressTable<uint>.Mask));
                    c.BranchIfTrue(lblFallback, masked);

                    Operand index3 = c.BitwiseAnd(c.ShiftRightUI(guestAddress, Const(39)), Const(0x1FFul));
                    Operand index2 = c.BitwiseAnd(c.ShiftRightUI(guestAddress, Const(30)), Const(0x1FFul));
                    Operand index1 = c.BitwiseAnd(c.ShiftRightUI(guestAddress, Const(21)), Const(0x1FFul));

                    Operand level3 = Const((long)c.FunctionTable.Base);
                    Operand level2 = c.Load(OperandType.I64, c.Add(level3, c.ShiftLeft(index3, Const(3))));
                    c.BranchIfFalse(lblFallback, level2);

                    Operand level1 = c.Load(OperandType.I64, c.Add(level2, c.ShiftLeft(index2, Const(3))));
                    c.BranchIfFalse(lblFallback, level1);

                    Operand level0 = c.Load(OperandType.I64, c.Add(level1, c.ShiftLeft(index1, Const(3))));
                    c.BranchIfFalse(lblFallback, level0);

                    offsetAddr = c.Add(level0, c.ShiftLeft(index0, Const(2)));
                }

                Operand offset = c.Load(OperandType.I32, offsetAddr);
                c.BranchIf(lblFallback, offset, Const(uint.MaxValue), Comparison.Equal);

                hostAddress = c.Add(Const((long)JitCache.Base), c.ZeroExtend32(OperandType.I64, offset));
                EmitTranslationSwitch(c, hostAddress, isJump);
                c.Branch(lblEnd);

                c.MarkLabel(lblFallback, BasicBlockFrequency.Cold);
            }

            hostAddress = c.Call(typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetFunctionAddress)), guestAddress);
            EmitTranslationSwitch(c, hostAddress, isJump);

            c.MarkLabel(lblEnd);
        }

        private static void EmitTranslationSwitch(ArmEmitterContext context, Operand hostAddress, bool isJump)
        {
            Operand nativeContext = context.LoadArgument(OperandType.I64, 0);

            if (isJump)
            {
                context.Tailcall(hostAddress, nativeContext);
            }
            else
            {
                OpCode op = context.CurrOp;

                Operand returnAddress = context.Call(hostAddress, OperandType.I64, nativeContext);

                context.LoadFromContext();

                // Note: The return value of a translated function is always an Int64 with the address execution has
                // returned to. We expect this address to be immediately after the current instruction, if it isn't we
                // keep returning until we reach the dispatcher.
                Operand nextAddr = Const((long)op.Address + op.OpCodeSizeInBytes);

                // Try to continue within this block.
                // If the return address isn't to our next instruction, we need to return so the JIT can figure out
                // what to do.
                Operand lblContinue = context.GetLabel(nextAddr.Value);

                // We need to clear out the call flag for the return address before comparing it.
                context.BranchIf(lblContinue, returnAddress, nextAddr, Comparison.Equal, BasicBlockFrequency.Cold);

                context.Return(returnAddress);
            }
        }
    }
}
