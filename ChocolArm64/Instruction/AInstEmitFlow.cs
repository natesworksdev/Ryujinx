using ChocolArm64.Decoder;
using ChocolArm64.State;
using ChocolArm64.Translation;
using System.Reflection.Emit;

namespace ChocolArm64.Instruction
{
    static partial class AInstEmit
    {
        public static void B(AILEmitterCtx Context)
        {
            AOpCodeBImmAl Op = (AOpCodeBImmAl)Context.CurrOp;

            if (Context.CurrBlock.Branch != null)
            {
                Context.Emit(OpCodes.Br, Context.GetLabel(Op.Imm));
            }
            else
            {
                Context.EmitStoreState();
                Context.EmitLdc_I8(Op.Imm);

                Context.Emit(OpCodes.Br, Context.ExitLabel);
            }
        }

        public static void B_Cond(AILEmitterCtx Context)
        {
            AOpCodeBImmCond Op = (AOpCodeBImmCond)Context.CurrOp;

            AILLabel LblTaken;

            if (Context.CurrBlock.Branch != null)
            {
                LblTaken = Context.GetLabel(Op.Imm);
            }
            else
            {
                LblTaken = new AILLabel();
            }

            Context.EmitCondBranch(LblTaken, Op.Cond);

            if (Context.CurrBlock.Next   == null ||
                Context.CurrBlock.Branch == null)
            {
                EmitBranchPaths(Context, LblTaken);
            }
        }

        public static void Bl(AILEmitterCtx Context)
        {
            AOpCodeBImmAl Op = (AOpCodeBImmAl)Context.CurrOp;

            Context.EmitLdc_I(Op.Position + 4);
            Context.EmitStint(AThreadState.LRIndex);
            Context.EmitStoreState();

            if (Context.TryOptEmitSubroutineCall())
            {
                //Note: the return value of the called method will be placed
                //at the Stack, the return value is always a Int64 with the
                //return address of the function. We check if the address is
                //correct, if it isn't we keep returning until we reach the dispatcher.
                Context.Emit(OpCodes.Dup);

                Context.EmitLdc_I8(Op.Position + 4);

                AILLabel LblContinue = new AILLabel();

                Context.Emit(OpCodes.Beq_S, LblContinue);
                Context.Emit(OpCodes.Ret);

                Context.MarkLabel(LblContinue);

                Context.Emit(OpCodes.Pop);

                Context.EmitLoadState(Context.CurrBlock.Next);
            }
            else
            {
                Context.EmitLdc_I8(Op.Imm);

                Context.Emit(OpCodes.Ret);
            }
        }

        public static void Blr(AILEmitterCtx Context)
        {
            AOpCodeBReg Op = (AOpCodeBReg)Context.CurrOp;

            Context.EmitLdc_I(Op.Position + 4);
            Context.EmitStint(AThreadState.LRIndex);
            Context.EmitStoreState();
            Context.EmitLdintzr(Op.Rn);

            Context.Emit(OpCodes.Ret);
        }

        public static void Br(AILEmitterCtx Context)
        {
            AOpCodeBReg Op = (AOpCodeBReg)Context.CurrOp;

            Context.EmitStoreState();
            Context.EmitLdintzr(Op.Rn);

            Context.Emit(OpCodes.Ret);
        }

        public static void Cbnz(AILEmitterCtx Context) => EmitCb(Context, OpCodes.Bne_Un);
        public static void Cbz(AILEmitterCtx Context)  => EmitCb(Context, OpCodes.Beq);

        private static void EmitCb(AILEmitterCtx Context, OpCode ILOp)
        {
            AOpCodeBImmCmp Op = (AOpCodeBImmCmp)Context.CurrOp;

            Context.EmitLdintzr(Op.Rt);
            Context.EmitLdc_I(0);

            EmitBranch(Context, ILOp);
        }

        public static void Ret(AILEmitterCtx Context)
        {
            Context.EmitStoreState();
            Context.EmitLdint(AThreadState.LRIndex);

            Context.Emit(OpCodes.Ret);
        }

        public static void Tbnz(AILEmitterCtx Context) => EmitTb(Context, OpCodes.Bne_Un);
        public static void Tbz(AILEmitterCtx Context)  => EmitTb(Context, OpCodes.Beq);

        private static void EmitTb(AILEmitterCtx Context, OpCode ILOp)
        {
            AOpCodeBImmTest Op = (AOpCodeBImmTest)Context.CurrOp;

            Context.EmitLdintzr(Op.Rt);
            Context.EmitLdc_I(1L << Op.Pos);

            Context.Emit(OpCodes.And);

            Context.EmitLdc_I(0);

            EmitBranch(Context, ILOp);
        }

        private static void EmitBranch(AILEmitterCtx Context, OpCode ILOp)
        {
            AOpCodeBImm Op = (AOpCodeBImm)Context.CurrOp;

            AILLabel LblTaken;

            if (Context.CurrBlock.Branch != null)
            {
                LblTaken = Context.GetLabel(Op.Imm);
            }
            else
            {
                LblTaken = new AILLabel();
            }

            Context.Emit(ILOp, LblTaken);

            if (Context.CurrBlock.Next   == null ||
                Context.CurrBlock.Branch == null)
            {
                EmitBranchPaths(Context, LblTaken);
            }
        }

        private static void EmitBranchPaths(AILEmitterCtx Context, AILLabel LblTaken)
        {
            AOpCodeBImm Op = (AOpCodeBImm)Context.CurrOp;

            AILLabel LblEnd = null;

            if (Context.CurrBlock.Next == null)
            {
                EmitBranchExit(Context, Op.Position + 4);
            }
            else
            {
                LblEnd = new AILLabel();

                Context.Emit(OpCodes.Br, LblEnd);
            }

            if (Context.CurrBlock.Branch == null)
            {
                Context.MarkLabel(LblTaken);

                EmitBranchExit(Context, Op.Imm);
            }

            if (LblEnd != null)
            {
                Context.MarkLabel(LblEnd);
            }
        }

        private static void EmitBranchExit(AILEmitterCtx Context, long Imm)
        {
            Context.EmitStoreState();
            Context.EmitLdc_I8(Imm);

            Context.Emit(OpCodes.Br, Context.ExitLabel);
        }
    }
}