using ChocolArm64.Decoder;
using ChocolArm64.Translation;

using static ChocolArm64.Instruction.AInstEmitSimdHelper;

namespace ChocolArm64.Instruction
{
    static partial class AInstEmit
    {
#region "Sha1"
        public static void Sha1c_V(AilEmitterCtx context)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            context.EmitLdvec(op.Rd);
            EmitVectorExtractZx(context, op.Rn, 0, 2);
            context.EmitLdvec(op.Rm);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.HashChoose));

            context.EmitStvec(op.Rd);
        }

        public static void Sha1h_V(AilEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            EmitVectorExtractZx(context, op.Rn, 0, 2);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.FixedRotate));

            EmitScalarSet(context, op.Rd, 2);
        }

        public static void Sha1m_V(AilEmitterCtx context)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            context.EmitLdvec(op.Rd);
            EmitVectorExtractZx(context, op.Rn, 0, 2);
            context.EmitLdvec(op.Rm);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.HashMajority));

            context.EmitStvec(op.Rd);
        }

        public static void Sha1p_V(AilEmitterCtx context)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            context.EmitLdvec(op.Rd);
            EmitVectorExtractZx(context, op.Rn, 0, 2);
            context.EmitLdvec(op.Rm);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.HashParity));

            context.EmitStvec(op.Rd);
        }

        public static void Sha1su0_V(AilEmitterCtx context)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            context.EmitLdvec(op.Rd);
            context.EmitLdvec(op.Rn);
            context.EmitLdvec(op.Rm);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.Sha1SchedulePart1));

            context.EmitStvec(op.Rd);
        }

        public static void Sha1su1_V(AilEmitterCtx context)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            context.EmitLdvec(op.Rd);
            context.EmitLdvec(op.Rn);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.Sha1SchedulePart2));

            context.EmitStvec(op.Rd);
        }
#endregion

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

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.Sha256SchedulePart1));

            context.EmitStvec(op.Rd);
        }

        public static void Sha256su1_V(AilEmitterCtx context)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            context.EmitLdvec(op.Rd);
            context.EmitLdvec(op.Rn);
            context.EmitLdvec(op.Rm);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.Sha256SchedulePart2));

            context.EmitStvec(op.Rd);
        }
#endregion
    }
}
