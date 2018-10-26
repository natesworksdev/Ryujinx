using ChocolArm64.Decoder;
using ChocolArm64.State;
using ChocolArm64.Translation;
using System.Reflection.Emit;

namespace ChocolArm64.Instruction
{
    internal static partial class AInstEmit
    {
        public static void B(AILEmitterCtx context)
        {
            AOpCodeBImmAl op = (AOpCodeBImmAl)context.CurrOp;

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

        public static void B_Cond(AILEmitterCtx context)
        {
            AOpCodeBImmCond op = (AOpCodeBImmCond)context.CurrOp;

            EmitBranch(context, op.Cond);
        }

        public static void Bl(AILEmitterCtx context)
        {
            AOpCodeBImmAl op = (AOpCodeBImmAl)context.CurrOp;

            context.EmitLdc_I(op.Position + 4);
            context.EmitStint(AThreadState.LrIndex);
            context.EmitStoreState();

            if (context.TryOptEmitSubroutineCall())
            {
                //Note: the return value of the called method will be placed
                //at the Stack, the return value is always a Int64 with the
                //return address of the function. We check if the address is
                //correct, if it isn't we keep returning until we reach the dispatcher.
                context.Emit(OpCodes.Dup);

                context.EmitLdc_I8(op.Position + 4);

                AILLabel lblContinue = new AILLabel();

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

        public static void Blr(AILEmitterCtx context)
        {
            AOpCodeBReg op = (AOpCodeBReg)context.CurrOp;

            context.EmitLdc_I(op.Position + 4);
            context.EmitStint(AThreadState.LrIndex);
            context.EmitStoreState();
            context.EmitLdintzr(op.Rn);

            context.Emit(OpCodes.Ret);
        }

        public static void Br(AILEmitterCtx context)
        {
            AOpCodeBReg op = (AOpCodeBReg)context.CurrOp;

            context.EmitStoreState();
            context.EmitLdintzr(op.Rn);

            context.Emit(OpCodes.Ret);
        }

        public static void Cbnz(AILEmitterCtx context)
        {
            EmitCb(context, OpCodes.Bne_Un);
        }

        public static void Cbz(AILEmitterCtx context)
        {
            EmitCb(context, OpCodes.Beq);
        }

        private static void EmitCb(AILEmitterCtx context, OpCode ilOp)
        {
            AOpCodeBImmCmp op = (AOpCodeBImmCmp)context.CurrOp;

            context.EmitLdintzr(op.Rt);
            context.EmitLdc_I(0);

            EmitBranch(context, ilOp);
        }

        public static void Ret(AILEmitterCtx context)
        {
            context.EmitStoreState();
            context.EmitLdint(AThreadState.LrIndex);

            context.Emit(OpCodes.Ret);
        }

        public static void Tbnz(AILEmitterCtx context)
        {
            EmitTb(context, OpCodes.Bne_Un);
        }

        public static void Tbz(AILEmitterCtx context)
        {
            EmitTb(context, OpCodes.Beq);
        }

        private static void EmitTb(AILEmitterCtx context, OpCode ilOp)
        {
            AOpCodeBImmTest op = (AOpCodeBImmTest)context.CurrOp;

            context.EmitLdintzr(op.Rt);
            context.EmitLdc_I(1L << op.Pos);

            context.Emit(OpCodes.And);

            context.EmitLdc_I(0);

            EmitBranch(context, ilOp);
        }

        private static void EmitBranch(AILEmitterCtx context, ACond cond)
        {
            AOpCodeBImm op = (AOpCodeBImm)context.CurrOp;

            if (context.CurrBlock.Next   != null &&
                context.CurrBlock.Branch != null)
            {
                context.EmitCondBranch(context.GetLabel(op.Imm), cond);
            }
            else
            {
                context.EmitStoreState();

                AILLabel lblTaken = new AILLabel();

                context.EmitCondBranch(lblTaken, cond);

                context.EmitLdc_I8(op.Position + 4);

                context.Emit(OpCodes.Ret);

                context.MarkLabel(lblTaken);

                context.EmitLdc_I8(op.Imm);

                context.Emit(OpCodes.Ret);
            }
        }

        private static void EmitBranch(AILEmitterCtx context, OpCode ilOp)
        {
            AOpCodeBImm op = (AOpCodeBImm)context.CurrOp;

            if (context.CurrBlock.Next   != null &&
                context.CurrBlock.Branch != null)
            {
                context.Emit(ilOp, context.GetLabel(op.Imm));
            }
            else
            {
                context.EmitStoreState();

                AILLabel lblTaken = new AILLabel();

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