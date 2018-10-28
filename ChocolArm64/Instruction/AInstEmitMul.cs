using ChocolArm64.Decoder;
using ChocolArm64.Translation;
using System.Reflection.Emit;

namespace ChocolArm64.Instruction
{
    static partial class AInstEmit
    {
        public static void Madd(AilEmitterCtx context) => EmitMul(context, OpCodes.Add);
        public static void Msub(AilEmitterCtx context) => EmitMul(context, OpCodes.Sub);

        private static void EmitMul(AilEmitterCtx context, OpCode ilOp)
        {
            AOpCodeMul op = (AOpCodeMul)context.CurrOp;

            context.EmitLdintzr(op.Ra);
            context.EmitLdintzr(op.Rn);
            context.EmitLdintzr(op.Rm);

            context.Emit(OpCodes.Mul);
            context.Emit(ilOp);

            context.EmitStintzr(op.Rd);
        }

        public static void Smaddl(AilEmitterCtx context) => EmitMull(context, OpCodes.Add, true);
        public static void Smsubl(AilEmitterCtx context) => EmitMull(context, OpCodes.Sub, true);
        public static void Umaddl(AilEmitterCtx context) => EmitMull(context, OpCodes.Add, false);
        public static void Umsubl(AilEmitterCtx context) => EmitMull(context, OpCodes.Sub, false);

        private static void EmitMull(AilEmitterCtx context, OpCode addSubOp, bool signed)
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

        public static void Smulh(AilEmitterCtx context)
        {
            AOpCodeMul op = (AOpCodeMul)context.CurrOp;

            context.EmitLdintzr(op.Rn);
            context.EmitLdintzr(op.Rm);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.SMulHi128));

            context.EmitStintzr(op.Rd);
        }

        public static void Umulh(AilEmitterCtx context)
        {
            AOpCodeMul op = (AOpCodeMul)context.CurrOp;

            context.EmitLdintzr(op.Rn);
            context.EmitLdintzr(op.Rm);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.UMulHi128));

            context.EmitStintzr(op.Rd);
        }
    }
}