using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System.Reflection;

using static ARMeilleure.Instructions.InstEmitHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit
    {
        public static void Aesd_V(EmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand d = GetVec(op.Rd);
            Operand n = GetVec(op.Rn);

            MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.Decrypt));

            context.Copy(d, context.Call(info, d, n));
        }

        public static void Aese_V(EmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand d = GetVec(op.Rd);
            Operand n = GetVec(op.Rn);

            MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.Encrypt));

            context.Copy(d, context.Call(info, d, n));
        }

        public static void Aesimc_V(EmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand n = GetVec(op.Rn);

            MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.InverseMixColumns));

            context.Copy(GetVec(op.Rd), context.Call(info, n));
        }

        public static void Aesmc_V(EmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand n = GetVec(op.Rn);

            MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.MixColumns));

            context.Copy(GetVec(op.Rd), context.Call(info, n));
        }
    }
}
