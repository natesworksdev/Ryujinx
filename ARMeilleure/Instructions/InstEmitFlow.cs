using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;
using ARMeilleure.Translation.PTC;

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

            context.Branch(context.GetLabel((ulong)op.Immediate));
        }

        public static void B_Cond(ArmEmitterContext context)
        {
            OpCodeBImmCond op = (OpCodeBImmCond)context.CurrOp;

            EmitBranch(context, op.Cond);
        }

        public static void Bl(ArmEmitterContext context)
        {
            OpCodeBImmAl op = (OpCodeBImmAl)context.CurrOp;

            ulong address = op.Address + 4;

            Operand addressOp = !context.HasTtc
                ? Const(address)
                : Const(address, new Symbol(SymbolType.DynFunc, context.GetOffset(address)));

            context.Copy(GetIntOrZR(context, RegisterAlias.Lr), addressOp);

            EmitCall(context, (ulong)op.Immediate);
        }

        public static void Blr(ArmEmitterContext context)
        {
            OpCodeBReg op = (OpCodeBReg)context.CurrOp;

            ulong address = op.Address + 4;

            Operand addressOp = !context.HasTtc
                ? Const(address)
                : Const(address, new Symbol(SymbolType.DynFunc, context.GetOffset(address)));

            context.Copy(GetIntOrZR(context, RegisterAlias.Lr), addressOp);

            Operand n = context.Copy(GetIntOrZR(context, op.Rn));

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

            EmitCondBranch(context, context.GetLabel((ulong)op.Immediate), cond);
        }

        private static void EmitBranch(ArmEmitterContext context, Operand value, bool onNotZero)
        {
            OpCodeBImm op = (OpCodeBImm)context.CurrOp;

            Operand lblTarget = context.GetLabel((ulong)op.Immediate);

            if (onNotZero)
            {
                context.BranchIfTrue(lblTarget, value);
            }
            else
            {
                context.BranchIfFalse(lblTarget, value);
            }
        }
    }
}