using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;

using static ARMeilleure.IntermediateRepresentation.Operand.Factory;

namespace ARMeilleure.Instructions
{
    static class InstEmitSimdHashHelper
    {
        public static Operand EmitSha256h(ArmEmitterContext context, Operand x, Operand y, Operand w, bool part2)
        {
            if (Optimizations.UseSha)
            {
                Operand src1 = context.AddIntrinsic(Intrinsic.X86Shufps, y, x, Const(0xbb));
                Operand src2 = context.AddIntrinsic(Intrinsic.X86Shufps, y, x, Const(0x11));
                Operand w2 = context.AddIntrinsic(Intrinsic.X86Punpckhqdq, w, w);

                Operand round2 = context.AddIntrinsic(Intrinsic.X86Sha256Rnds2, src1, src2, w);
                Operand round4 = context.AddIntrinsic(Intrinsic.X86Sha256Rnds2, src2, round2, w2);
                
                Operand res = context.AddIntrinsic(Intrinsic.X86Shufps, round4, round2, Const(part2 ? 0x11 : 0xbb));
                
                return res;
            }

            String method = part2 ? nameof(SoftFallback.HashUpper) : nameof(SoftFallback.HashLower);
            return context.Call(typeof(SoftFallback).GetMethod(method), x, y, w);
        }
    }
}