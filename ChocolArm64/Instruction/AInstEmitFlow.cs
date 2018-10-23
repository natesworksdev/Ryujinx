using ChocolArm64.Decoder;
using ChocolArm64.State;
using ChocolArm64.Translation;
using System.Reflection.Emit;

namespace ChocolArm64.Instruction
{
    static partial class AInstEmit
    {
        public static void B(AilEmitterCtx context)
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

        public static void B_Cond(AilEmitterCtx context)
        {
            AOpCodeBImmCond op = (AOpCodeBImmCond)context.CurrOp;

            EmitBranch(context, op.Cond);
        }

        public static void Bl(AilEmitterCtx context)
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

                AilLabel lblContinue = new AilLabel();

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

        public static void Blr(AilEmitterCtx context)
        {
            AOpCodeBReg op = (AOpCodeBReg)context.CurrOp;

            context.EmitLdc_I(op.Position + 4);
            context.EmitStint(AThreadState.LrIndex);
            context.EmitStoreState();
            context.EmitLdintzr(op.Rn);

            context.Emit(OpCodes.Ret);
        }

        public static void Br(AilEmitterCtx context)
        {
            AOpCodeBReg op = (AOpCodeBReg)context.CurrOp;

            context.EmitStoreState();
            context.EmitLdintzr(op.Rn);

            context.Emit(OpCodes.Ret);
        }

        public static void Cbnz(AilEmitterCtx context) => EmitCb(context, OpCodes.Bne_Un);
        public static void Cbz(AilEmitterCtx context)  => EmitCb(context, OpCodes.Beq);

        private static void EmitCb(AilEmitterCtx context, OpCode ilOp)
        {
            AOpCodeBImmCmp op = (AOpCodeBImmCmp)context.CurrOp;

            context.EmitLdintzr(op.Rt);
            context.EmitLdc_I(0);

            EmitBranch(context, ilOp);
        }

        public static void Ret(AilEmitterCtx context)
        {
            context.EmitStoreState();
            context.EmitLdint(AThreadState.LrIndex);

            context.Emit(OpCodes.Ret);
        }

        public static void Tbnz(AilEmitterCtx context) => EmitTb(context, OpCodes.Bne_Un);
        public static void Tbz(AilEmitterCtx context)  => EmitTb(context, OpCodes.Beq);

        private static void EmitTb(AilEmitterCtx context, OpCode ilOp)
        {
            AOpCodeBImmTest op = (AOpCodeBImmTest)context.CurrOp;

            context.EmitLdintzr(op.Rt);
            context.EmitLdc_I(1L << op.Pos);

            context.Emit(OpCodes.And);

            context.EmitLdc_I(0);

            EmitBranch(context, ilOp);
        }

        private static void EmitBranch(AilEmitterCtx context, ACond cond)
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

                AilLabel lblTaken = new AilLabel();

                context.EmitCondBranch(lblTaken, cond);

                context.EmitLdc_I8(op.Position + 4);

                context.Emit(OpCodes.Ret);

                context.MarkLabel(lblTaken);

                context.EmitLdc_I8(op.Imm);

                context.Emit(OpCodes.Ret);
            }
        }

        private static void EmitBranch(AilEmitterCtx context, OpCode ilOp)
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

                AilLabel lblTaken = new AilLabel();

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