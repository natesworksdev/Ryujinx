using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;

using static ARMeilleure.Instructions.InstEmitHelper;

namespace ARMeilleure.Instructions
{
    partial class InstEmit32
    {
        public static void Aesd_V(ArmEmitterContext context)
        {
            OpCode32Simd op = (OpCode32Simd)context.CurrOp;

            Operand d = GetVecA32(op.Qd);
            Operand n = GetVecA32(op.Qm);

            context.Copy(d, context.Call(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Decrypt)), d, n));
        }

        public static void Aese_V(ArmEmitterContext context)
        {
            OpCode32Simd op = (OpCode32Simd)context.CurrOp;

            Operand d = GetVecA32(op.Qd);
            Operand n = GetVecA32(op.Qm);

            context.Copy(d, context.Call(typeof(SoftFallback).GetMethod(nameof(SoftFallback.Encrypt)), d, n));
        }

        public static void Aesimc_V(ArmEmitterContext context)
        {
            OpCode32Simd op = (OpCode32Simd)context.CurrOp;

            Operand n = GetVecA32(op.Qm);

            context.Copy(GetVec(op.Qd), context.Call(typeof(SoftFallback).GetMethod(nameof(SoftFallback.InverseMixColumns)), n));
        }

        public static void Aesmc_V(ArmEmitterContext context)
        {
            OpCode32Simd op = (OpCode32Simd)context.CurrOp;

            Operand n = GetVecA32(op.Qm);

            context.Copy(GetVec(op.Qd), context.Call(typeof(SoftFallback).GetMethod(nameof(SoftFallback.MixColumns)), n));
        }
    }
}
