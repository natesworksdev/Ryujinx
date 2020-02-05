using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static class InstEmitFlowHelper
    {
        public const ulong CallFlag = 1;

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
            EmitJumpTableCall(context, Const(immediate));
        }

        private static void EmitNativeCall(ArmEmitterContext context, Operand funcAddr, bool isJump = false)
        {
            context.StoreToContext();
            Operand returnAddress;
            if (isJump)
            {
                context.Tailcall(funcAddr, context.LoadArgument(OperandType.I64, 0));
            } 
            else
            {
                returnAddress = context.Call(funcAddr, OperandType.I64, context.LoadArgument(OperandType.I64, 0));
                context.LoadFromContext();

                // InstEmitFlowHelper.EmitContinueOrReturnCheck(context, returnAddress);

                // If the return address isn't to our next instruction, we need to return to the JIT can figure out what to do.
                Operand continueLabel = Label();
                Operand next = Const(GetNextOpAddress(context.CurrOp));
                context.BranchIfTrue(continueLabel, context.ICompareEqual(context.BitwiseAnd(returnAddress, Const(~1L)), next));

                context.Return(returnAddress);

                context.MarkLabel(continueLabel);
            }
        }

        public static void EmitVirtualCall(ArmEmitterContext context, Operand target)
        {
            EmitVirtualCallOrJump(context, target, isJump: false);
        }

        public static void EmitVirtualJump(ArmEmitterContext context, Operand target, bool isReturn)
        {
            EmitVirtualCallOrJump(context, target, isJump: true, isReturn: isReturn);
        }

        private static void EmitVirtualCallOrJump(ArmEmitterContext context, Operand target, bool isJump, bool isReturn = false)
        {
            if (isReturn)
            {
                context.Return(target);
            }
            else
            {
                EmitJumpTableCall(context, target, isJump);
            }
        }

        public static void EmitContinueOrReturnCheck(ArmEmitterContext context, Operand retVal)
        {
            // Note: The return value of the called method will be placed
            // at the Stack, the return value is always a Int64 with the
            // return address of the function. We check if the address is
            // correct, if it isn't we keep returning until we reach the dispatcher.
            ulong nextAddr = GetNextOpAddress(context.CurrOp);

            if (context.CurrBlock.Next != null)
            {
                Operand lblContinue = Label();

                context.BranchIfTrue(lblContinue, context.ICompareEqual(retVal, Const(nextAddr)));

                context.Return(Const(nextAddr));

                context.MarkLabel(lblContinue);
            }
            else
            {
                context.Return(Const(nextAddr));
            }
        }

        private static ulong GetNextOpAddress(OpCode op)
        {
            return op.Address + (ulong)op.OpCodeSizeInBytes;
        }

        public static void EmitDynamicTableCall(ArmEmitterContext context, Operand tableAddress, Operand address, bool isJump)
        {
            if (address.Type == OperandType.I32)
            {
                address = context.ZeroExtend32(OperandType.I64, address);
            }

            // Loop over elements of the dynamic table. Unrolled loop.
            // TODO: different reserved size for jumps? Need to do some testing to see what is reasonable.

            Operand endLabel = Label();
            Operand fallbackLabel = Label();

            for (int i = 0; i < JumpTable.DynamicTableElems; i++) 
            {
                // TODO: USE COMPARE AND SWAP I64 TO ENSURE ATOMIC OPERATIONS

                Operand nextLabel = Label();

                // Load this entry from the table. 
                Operand entry = context.Load(OperandType.I64, tableAddress);

                // If it's 0, we can take this entry in the table.
                // (TODO: compare and exchange with our address _first_ when implemented, then just check if the entry is ours)
                Operand hasAddressLabel = Label();
                Operand gotTableLabel = Label();
                context.BranchIfTrue(hasAddressLabel, entry);

                // Take the entry.
                context.Store(tableAddress, address);
                context.Branch(gotTableLabel);

                context.MarkLabel(hasAddressLabel);

                // If there is an entry here, is it ours?
                context.BranchIfFalse(nextLabel, context.ICompareEqual(entry, address));

                context.MarkLabel(gotTableLabel);

                // It's ours, so what function is it pointing to?
                Operand missingFunctionLabel = Label();
                Operand targetFunctionPtr = context.Add(tableAddress, Const(8));
                Operand targetFunction = context.Load(OperandType.I64, targetFunctionPtr);
                context.BranchIfFalse(missingFunctionLabel, targetFunction);

                // Call the function.
                EmitNativeCall(context, targetFunction, isJump);
                context.Branch(endLabel);

                // We need to find the missing function. This can only be from a list of HighCq functions, which the JumpTable maintains.
                context.MarkLabel(missingFunctionLabel);
                Operand goodCallAddr = context.Call(new _U64_U64(context.JumpTable.TryGetFunction), address); // TODO: NativeInterface call to it? (easier to AOT)

                context.BranchIfFalse(fallbackLabel, goodCallAddr); // Fallback if it doesn't exist yet.

                // Call and save the function.
                context.Store(targetFunctionPtr, goodCallAddr);
                EmitNativeCall(context, goodCallAddr, isJump);
                context.Branch(endLabel);

                context.MarkLabel(nextLabel);
                tableAddress = context.Add(tableAddress, Const(16)); // Move to the next table entry.
            }

            context.MarkLabel(fallbackLabel);

            address = context.BitwiseOr(address, Const(address.Type, 1)); // Set call flag.

            Operand fallbackAddr = context.Call(new _U64_U64(NativeInterface.GetFunctionAddress), address);
            EmitNativeCall(context, fallbackAddr, isJump);

            context.MarkLabel(endLabel);
        }

        public static void EmitJumpTableCall(ArmEmitterContext context, Operand address, bool isJump = false)
        {
            // Does the call have a constant value, or can it be folded to one?
            // TODO: Constant folding. Indirect calls are slower in the best case and emit more code so we want to avoid them when possible.
            bool isConst = address.Kind == OperandKind.Constant;
            long constAddr = (long)address.Value;

            if (isJump || !isConst || !context.HighCq)
            {
                if (context.HighCq)
                {
                    // Virtual branch/call - store first used addresses on a small table for fast lookup.
                    int entry = context.JumpTable.ReserveDynamicEntry();

                    int jumpOffset = entry * JumpTable.JumpTableStride * JumpTable.DynamicTableElems;
                    Operand dynTablePtr = Const(context.JumpTable.DynamicPointer.ToInt64() + jumpOffset);

                    EmitDynamicTableCall(context, dynTablePtr, address, isJump);
                }
                else
                {
                    // Don't emit indirect calls or jumps if we're compiling in lowCq mode.
                    // This avoids wasting space on the jump and indirect tables.
                    context.Return(context.BitwiseOr(address, Const(address.Type, 1))); // Set call flag.
                }
            }
            else
            {
                int entry = context.JumpTable.ReserveTableEntry(context.BaseAddress & (~3L), constAddr);

                int jumpOffset = entry * JumpTable.JumpTableStride + 8; // Offset directly to the host address.

                // TODO: Portable jump table ptr with IR adding of the offset. Will be easy to relocate for things like AOT.
                Operand tableEntryPtr = Const(context.JumpTable.BasePointer.ToInt64() + jumpOffset);

                Operand funcAddr = context.Load(OperandType.I64, tableEntryPtr);

                Operand directCallLabel = Label();
                Operand endLabel = Label();

                // Host address in the table is 0 until the function is rejit.
                context.BranchIfTrue(directCallLabel, funcAddr);

                // Call the function through the translator until it is rejit.
                address = context.BitwiseOr(address, Const(address.Type, 1)); // Set call flag.
                Operand fallbackAddr = context.Call(new _U64_U64(NativeInterface.GetFunctionAddress), address);
                EmitNativeCall(context, fallbackAddr);

                context.Branch(endLabel);

                context.MarkLabel(directCallLabel);

                EmitNativeCall(context, funcAddr); // Call the function directly if it is present in the entry.

                context.MarkLabel(endLabel);
            }
        }
    }
}
