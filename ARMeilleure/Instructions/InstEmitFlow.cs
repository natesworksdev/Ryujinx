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
        public static void B(EmitterContext context)
        {
            OpCodeBImmAl op = (OpCodeBImmAl)context.CurrOp;

            if (context.CurrBlock.Branch != null)
            {
                context.Branch(context.GetLabel((ulong)op.Immediate));
            }
            else
            {
                context.Return(Const(op.Immediate));
            }
        }

        public static void B_Cond(EmitterContext context)
        {
            OpCodeBImmCond op = (OpCodeBImmCond)context.CurrOp;

            EmitBranch(context, op.Cond);
        }

        public static void Bl(EmitterContext context)
        {
            OpCodeBImmAl op = (OpCodeBImmAl)context.CurrOp;

            context.Copy(GetIntOrZR(context, RegisterAlias.Lr), Const(op.Address + 4));

            EmitCall(context, (ulong)op.Immediate);
        }

        public static void Blr(EmitterContext context)
        {
            OpCodeBReg op = (OpCodeBReg)context.CurrOp;

            Operand n = context.Copy(GetIntOrZR(context, op.Rn));

            context.Copy(GetIntOrZR(context, RegisterAlias.Lr), Const(op.Address + 4));

            EmitVirtualCall(context, n);
        }

        public static void Br(EmitterContext context)
        {
            OpCodeBReg op = (OpCodeBReg)context.CurrOp;

            EmitVirtualJump(context, GetIntOrZR(context, op.Rn));
        }

        public static void Cbnz(EmitterContext context) => EmitCb(context, onNotZero: true);
        public static void Cbz(EmitterContext context)  => EmitCb(context, onNotZero: false);

        private static void EmitCb(EmitterContext context, bool onNotZero)
        {
            OpCodeBImmCmp op = (OpCodeBImmCmp)context.CurrOp;

            EmitBranch(context, GetIntOrZR(context, op.Rt), onNotZero);
        }

        public static void Ret(EmitterContext context)
        {
            context.Return(GetIntOrZR(context, RegisterAlias.Lr));
        }

        public static void Tbnz(EmitterContext context) => EmitTb(context, onNotZero: true);
        public static void Tbz(EmitterContext context)  => EmitTb(context, onNotZero: false);

        private static void EmitTb(EmitterContext context, bool onNotZero)
        {
            OpCodeBImmTest op = (OpCodeBImmTest)context.CurrOp;

            Operand value = context.BitwiseAnd(GetIntOrZR(context, op.Rt), Const(1L << op.Bit));

            EmitBranch(context, value, onNotZero);
        }

        private static void EmitBranch(EmitterContext context, Condition cond)
        {
            OpCodeBImm op = (OpCodeBImm)context.CurrOp;

            if (context.CurrBlock.Branch != null)
            {
                EmitCondBranch(context, context.GetLabel((ulong)op.Immediate), cond);

                if (context.CurrBlock.Next == null)
                {
                    context.Return(Const(op.Address + 4));
                }
            }
            else
            {
                Operand lblTaken = Label();

                EmitCondBranch(context, lblTaken, cond);

                context.Return(Const(op.Address + 4));

                context.MarkLabel(lblTaken);

                context.Return(Const(op.Immediate));
            }
        }

        private static void EmitBranch(EmitterContext context, Operand value, bool onNotZero)
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
                    context.Return(Const(op.Address + 4));
                }
            }
            else
            {
                Operand lblTaken = Label();

                if (onNotZero)
                {
                    context.BranchIfTrue(lblTaken, value);
                }
                else
                {
                    context.BranchIfFalse(lblTaken, value);
                }

                context.Return(Const(op.Address + 4));

                context.MarkLabel(lblTaken);

                context.Return(Const(op.Immediate));
            }
        }
    }
}