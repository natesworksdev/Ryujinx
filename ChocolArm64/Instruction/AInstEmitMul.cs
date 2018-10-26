using ChocolArm64.Decoder;
using ChocolArm64.Translation;
using System.Reflection.Emit;

namespace ChocolArm64.Instruction
{
    internal static partial class AInstEmit
    {
        public static void Madd(AILEmitterCtx context)
        {
            EmitMul(context, OpCodes.Add);
        }

        public static void Msub(AILEmitterCtx context)
        {
            EmitMul(context, OpCodes.Sub);
        }

        private static void EmitMul(AILEmitterCtx context, OpCode ilOp)
        {
            AOpCodeMul op = (AOpCodeMul)context.CurrOp;

            context.EmitLdintzr(op.Ra);
            context.EmitLdintzr(op.Rn);
            context.EmitLdintzr(op.Rm);

            context.Emit(OpCodes.Mul);
            context.Emit(ilOp);

            context.EmitStintzr(op.Rd);
        }

        public static void Smaddl(AILEmitterCtx context)
        {
            EmitMull(context, OpCodes.Add, true);
        }

        public static void Smsubl(AILEmitterCtx context)
        {
            EmitMull(context, OpCodes.Sub, true);
        }

        public static void Umaddl(AILEmitterCtx context)
        {
            EmitMull(context, OpCodes.Add, false);
        }

        public static void Umsubl(AILEmitterCtx context)
        {
            EmitMull(context, OpCodes.Sub, false);
        }

        private static void EmitMull(AILEmitterCtx context, OpCode addSubOp, bool signed)
        {
            AOpCodeMul op = (AOpCodeMul)context.CurrOp;

            OpCode castOp = signed
                ? OpCodes.Conv_I8
                : OpCodes.Conv_U8;

            context.EmitLdintzr(op.Ra);
            context.EmitLdintzr(op.Rn);

            context.Emit(OpCodes.Conv_I4);
            context.Emit(castOp);

            context.EmitLdintzr(op.Rm);

            context.Emit(OpCodes.Conv_I4);
            context.Emit(castOp);
            context.Emit(OpCodes.Mul);

            context.Emit(addSubOp);

            context.EmitStintzr(op.Rd);
        }

        public static void Smulh(AILEmitterCtx context)
        {
            AOpCodeMul op = (AOpCodeMul)context.CurrOp;

            context.EmitLdintzr(op.Rn);
            context.EmitLdintzr(op.Rm);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.SMulHi128));

            context.EmitStintzr(op.Rd);
        }

        public static void Umulh(AILEmitterCtx context)
        {
            AOpCodeMul op = (AOpCodeMul)context.CurrOp;

            context.EmitLdintzr(op.Rn);
            context.EmitLdintzr(op.Rm);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.UMulHi128));

            context.EmitStintzr(op.Rd);
        }
    }
}