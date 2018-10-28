using ChocolArm64.Decoder;
using ChocolArm64.Translation;

namespace ChocolArm64.Instruction
{
    static partial class AInstEmit
    {
        public static void Aesd_V(AilEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            context.EmitLdvec(op.Rd);
            context.EmitLdvec(op.Rn);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.Decrypt));

            context.EmitStvec(op.Rd);
        }

        public static void Aese_V(AilEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            context.EmitLdvec(op.Rd);
            context.EmitLdvec(op.Rn);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.Encrypt));

            context.EmitStvec(op.Rd);
        }

        public static void Aesimc_V(AilEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            context.EmitLdvec(op.Rn);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.InverseMixColumns));

            context.EmitStvec(op.Rd);
        }

        public static void Aesmc_V(AilEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            context.EmitLdvec(op.Rn);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.MixColumns));

            context.EmitStvec(op.Rd);
        }
    }
}
