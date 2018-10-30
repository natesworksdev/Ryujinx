using ChocolArm64.Decoders;
using ChocolArm64.State;
using ChocolArm64.Translation;
using System.Reflection.Emit;

namespace ChocolArm64.Instructions
{
    static partial class InstEmit
    {
        public static void B(ILEmitterCtx context)
        {
            OpCodeBImmAl op = (OpCodeBImmAl)context.CurrOp;

            if (context.CurrBlock.Branch != null)
            {
                context.Emit(OpCodes.Br, context.GetLabel(op.Imm));
            }
            else
            {
                context.EmitStoreState();
                context.EmitLdc_I8(op.Imm);

                context.Emit(OpCodes.Ret);
            }
        }

        public static void B_Cond(ILEmitterCtx context)
        {
            OpCodeBImmCond op = (OpCodeBImmCond)context.CurrOp;

            EmitBranch(context, op.Cond);
        }

        public static void Bl(ILEmitterCtx context)
        {
            OpCodeBImmAl op = (OpCodeBImmAl)context.CurrOp;

            context.EmitLdc_I(op.Position + 4);
            context.EmitStint(CpuThreadState.LrIndex);
            context.EmitStoreState();

            if (context.TryOptEmitSubroutineCall())
            {
                //Note: the return value of the called method will be placed
                //at the Stack, the return value is always a Int64 with the
                //return address of the function. We check if the address is
                //correct, if it isn't we keep returning until we reach the dispatcher.
                context.Emit(OpCodes.Dup);

                context.EmitLdc_I8(op.Position + 4);

                ILLabel lblContinue = new ILLabel();

                context.Emit(OpCodes.Beq_S, lblContinue);
                context.Emit(OpCodes.Ret);

                context.MarkLabel(lblContinue);

                context.Emit(OpCodes.Pop);

                context.EmitLoadState(context.CurrBlock.Next);
            }
            else
            {
                context.EmitLdc_I8(op.Imm);

                context.Emit(OpCodes.Ret);
            }
        }

        public static void Blr(ILEmitterCtx context)
        {
            OpCodeBReg op = (OpCodeBReg)context.CurrOp;

            context.EmitLdc_I(op.Position + 4);
            context.EmitStint(CpuThreadState.LrIndex);
            context.EmitStoreState();
            context.EmitLdintzr(op.Rn);

            context.Emit(OpCodes.Ret);
        }

        public static void Br(ILEmitterCtx context)
        {
            OpCodeBReg op = (OpCodeBReg)context.CurrOp;

            context.EmitStoreState();
            context.EmitLdintzr(op.Rn);

            context.Emit(OpCodes.Ret);
        }

        public static void Cbnz(ILEmitterCtx context) => EmitCb(context, OpCodes.Bne_Un);
        public static void Cbz(ILEmitterCtx context)  => EmitCb(context, OpCodes.Beq);

        private static void EmitCb(ILEmitterCtx context, OpCode ilOp)
        {
            OpCodeBImmCmp op = (OpCodeBImmCmp)context.CurrOp;

            context.EmitLdintzr(op.Rt);
            context.EmitLdc_I(0);

            EmitBranch(context, ilOp);
        }

        public static void Ret(ILEmitterCtx context)
        {
            context.EmitStoreState();
            context.EmitLdint(CpuThreadState.LrIndex);

            context.Emit(OpCodes.Ret);
        }

        public static void Tbnz(ILEmitterCtx context) => EmitTb(context, OpCodes.Bne_Un);
        public static void Tbz(ILEmitterCtx context)  => EmitTb(context, OpCodes.Beq);

        private static void EmitTb(ILEmitterCtx context, OpCode ilOp)
        {
            OpCodeBImmTest op = (OpCodeBImmTest)context.CurrOp;

            context.EmitLdintzr(op.Rt);
            context.EmitLdc_I(1L << op.Pos);

            context.Emit(OpCodes.And);

            context.EmitLdc_I(0);

            EmitBranch(context, ilOp);
        }

        private static void EmitBranch(ILEmitterCtx context, Cond cond)
        {
            OpCodeBImm op = (OpCodeBImm)context.CurrOp;

            if (context.CurrBlock.Next   != null &&
                context.CurrBlock.Branch != null)
            {
                context.EmitCondBranch(context.GetLabel(op.Imm), cond);
            }
            else
            {
                context.EmitStoreState();

                ILLabel lblTaken = new ILLabel();

                context.EmitCondBranch(lblTaken, cond);

                context.EmitLdc_I8(op.Position + 4);

                context.Emit(OpCodes.Ret);

                context.MarkLabel(lblTaken);

                context.EmitLdc_I8(op.Imm);

                context.Emit(OpCodes.Ret);
            }
        }

        private static void EmitBranch(ILEmitterCtx context, OpCode ilOp)
        {
            OpCodeBImm op = (OpCodeBImm)context.CurrOp;

            if (context.CurrBlock.Next   != null &&
                context.CurrBlock.Branch != null)
            {
                context.Emit(ilOp, context.GetLabel(op.Imm));
            }
            else
            {
                context.EmitStoreState();

                ILLabel lblTaken = new ILLabel();

                context.Emit(ilOp, lblTaken);

                context.EmitLdc_I8(op.Position + 4);

                context.Emit(OpCodes.Ret);

                context.MarkLabel(lblTaken);

                context.EmitLdc_I8(op.Imm);

                context.Emit(OpCodes.Ret);
            }
        }
    }
}