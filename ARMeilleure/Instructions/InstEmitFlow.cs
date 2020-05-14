using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;

using static ARMeilleure.Instructions.InstEmitFlowHelper;
using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit
    {
        public static void B(ArmEmitterContext context)
        {
            OpCodeBImmAl op = (OpCodeBImmAl)context.CurrOp;

            if (context.CurrBlock.Branch != null)
            {
                context.Branch(context.GetLabel((ulong)op.Immediate));
            }
            else
            {
                EmitTailContinue(context, Const(op.Immediate), context.CurrBlock.TailCall);
            }
        }

        public static void B_Cond(ArmEmitterContext context)
        {
            OpCodeBImmCond op = (OpCodeBImmCond)context.CurrOp;

            EmitBranch(context, op.Cond);
        }

        public static void Bl(ArmEmitterContext context)
        {
            OpCodeBImmAl op = (OpCodeBImmAl)context.CurrOp;

            context.Copy(GetIntOrZR(context, RegisterAlias.Lr), Const(op.Address + 4));

            EmitCall(context, (ulong)op.Immediate);
        }

        public static void Blr(ArmEmitterContext context)
        {
            OpCodeBReg op = (OpCodeBReg)context.CurrOp;

            Operand n = context.Copy(GetIntOrZR(context, op.Rn));

            context.Copy(GetIntOrZR(context, RegisterAlias.Lr), Const(op.Address + 4));

            EmitVirtualCall(context, n);
        }

        public static void Br(ArmEmitterContext context)
        {
            OpCodeBReg op = (OpCodeBReg)context.CurrOp;

            EmitVirtualJump(context, GetIntOrZR(context, op.Rn), op.Rn == RegisterAlias.Lr);
        }

        public static void Cbnz(ArmEmitterContext context) => EmitCb(context, onNotZero: true);
        public static void Cbz(ArmEmitterContext context)  => EmitCb(context, onNotZero: false);

        private static void EmitCb(ArmEmitterContext context, bool onNotZero)
        {
            OpCodeBImmCmp op = (OpCodeBImmCmp)context.CurrOp;

            EmitBranch(context, GetIntOrZR(context, op.Rt), onNotZero);
        }

        public static void Ret(ArmEmitterContext context)
        {
            OpCodeBReg op = (OpCodeBReg)context.CurrOp;

            context.Return(GetIntOrZR(context, op.Rn));
        }

        public static void Tbnz(ArmEmitterContext context) => EmitTb(context, onNotZero: true);
        public static void Tbz(ArmEmitterContext context)  => EmitTb(context, onNotZero: false);

        private static void EmitTb(ArmEmitterContext context, bool onNotZero)
        {
            OpCodeBImmTest op = (OpCodeBImmTest)context.CurrOp;

            Operand value = context.BitwiseAnd(GetIntOrZR(context, op.Rt), Const(1L << op.Bit));

            EmitBranch(context, value, onNotZero);
        }

        private static void EmitBranch(ArmEmitterContext context, Condition cond)
        {
            OpCodeBImm op = (OpCodeBImm)context.CurrOp;

            if (context.CurrBlock.Branch != null)
            {
                EmitCondBranch(context, context.GetLabel((ulong)op.Immediate), cond);

                if (context.CurrBlock.Next == null)
                {
                    EmitTailContinue(context, Const(op.Address + 4));
                }
            }
            else
            {
                Operand lblFallthrough = Label();

                // The condition is inverted such that the CurrBlock.Next block is emitted below the CurrBlock.Branch
                // block. Sometimes CurrBlock.Branch will be null but not CurrBlock.Next, and this arrangement allows 
                // us to emit the CurrBlock.Next block without any Label book keeping.
                // 
                // Usually happens when the TailCallRemover kicked in.
                EmitCondBranch(context, lblFallthrough, cond.Invert());

                EmitTailContinue(context, Const(op.Immediate));

                context.MarkLabel(lblFallthrough);

                if (context.CurrBlock.Next == null)
                {
                    EmitTailContinue(context, Const(op.Address + 4));
                }
            }
        }

        private static void EmitBranch(ArmEmitterContext context, Operand value, bool onNotZero)
        {
            OpCodeBImm op = (OpCodeBImm)context.CurrOp;

            if (context.CurrBlock.Branch != null)
            {
                Operand lblTarget = context.GetLabel((ulong)op.Immediate);

                if (onNotZero)
                {
                    context.BranchIfTrue(lblTarget, value);
                }
                else
                {
                    context.BranchIfFalse(lblTarget, value);
                }

                if (context.CurrBlock.Next == null)
                {
                    EmitTailContinue(context, Const(op.Address + 4));
                }
            }
            else
            {
                Operand lblFallthrough = Label();

                // See EmitBranch(ArmEmitterContext context, Condition cond) for why the condition is inverted.
                if (onNotZero)
                {
                    context.BranchIfFalse(lblFallthrough, value);
                }
                else
                {
                    context.BranchIfTrue(lblFallthrough, value);
                }

                EmitTailContinue(context, Const(op.Immediate));

                context.MarkLabel(lblFallthrough);

                if (context.CurrBlock.Next == null)
                {
                    EmitTailContinue(context, Const(op.Address + 4));
                }
            }
        }
    }
}