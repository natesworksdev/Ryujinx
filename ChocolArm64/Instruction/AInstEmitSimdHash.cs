using ChocolArm64.Decoder;
using ChocolArm64.Translation;

namespace ChocolArm64.Instruction
{
    static partial class AInstEmit
    {
#region "Sha256"
        public static void Sha256h_V(AilEmitterCtx context)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            context.EmitLdvec(op.Rd);
            context.EmitLdvec(op.Rn);
            context.EmitLdvec(op.Rm);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.HashLower));

            context.EmitStvec(op.Rd);
        }

        public static void Sha256h2_V(AilEmitterCtx context)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            context.EmitLdvec(op.Rd);
            context.EmitLdvec(op.Rn);
            context.EmitLdvec(op.Rm);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.HashUpper));

            context.EmitStvec(op.Rd);
        }

        public static void Sha256su0_V(AilEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            context.EmitLdvec(op.Rd);
            context.EmitLdvec(op.Rn);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.SchedulePart1));

            context.EmitStvec(op.Rd);
        }

        public static void Sha256su1_V(AilEmitterCtx context)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            context.EmitLdvec(op.Rd);
            context.EmitLdvec(op.Rn);
            context.EmitLdvec(op.Rm);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.SchedulePart2));

            context.EmitStvec(op.Rd);
        }
#endregion
    }
}
