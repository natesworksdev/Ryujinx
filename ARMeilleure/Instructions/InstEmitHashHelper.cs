// https://www.intel.com/content/dam/www/public/us/en/documents/white-papers/fast-crc-computation-generic-polynomials-pclmulqdq-paper.pdf

using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System.Diagnostics;

using static ARMeilleure.IntermediateRepresentation.OperandHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;

namespace ARMeilleure.Instructions
{
    static class InstEmitHashHelper
    {
        public const uint Crc32RevPoly = 0xedb88320;
        public const uint Crc32cRevPoly = 0x82f63b78;

        public static Operand EmitCrc32(ArmEmitterContext context, Operand crc, Operand value, int size, bool castagnoli)
        {
            Debug.Assert(crc.Type == OperandType.I32);
            Debug.Assert(size >= 0 && size < 4);

            if (castagnoli && Optimizations.UseSse42)
            {
                Intrinsic op = size switch
                {
                    0 => Intrinsic.X86Crc32_8,
                    1 => Intrinsic.X86Crc32_16,
                    _ => Intrinsic.X86Crc32,
                };

                return context.AddIntrinsicInt(op, crc, value);
            }
            else if (Optimizations.UsePclmulqdq)
            {
                return size switch
                {
                    3 => EmitCrc32Optimized64(context, crc, value, castagnoli),
                    _ => EmitCrc32Optimized(context, crc, value, castagnoli, size),
                };
            }
            else
            {
                Operand poly = Const(castagnoli ? Crc32cRevPoly : Crc32RevPoly);
                int bytes = 1 << size;
                Operand one = Const(1);

                for (int i = 0; i < bytes; i++)
                {
                    Operand val = context.ZeroExtend8(OperandType.I32, context.ShiftRightUI(value, Const(i * 8)));
                    crc = context.BitwiseExclusiveOr(crc, val);
                    for (int k = 0; k < 8; k++)
                    {
                        // crc = (crc >> 1) ^ (poly & (0 - (crc & 1)));
                        crc = context.BitwiseExclusiveOr(context.ShiftRightUI(crc, one), context.BitwiseAnd(poly, context.Negate(context.BitwiseAnd(crc, one))));
                    }
                }

                return crc;
            }
        }

        private static Operand EmitCrc32Optimized(ArmEmitterContext context, Operand crc, Operand data, bool castagnoli, int size)
        {
            long mu = castagnoli ? 0x0DEA713F1 : 0x1F7011641; // mu' = floor(x^64/P(x))'
            long polynomial = castagnoli ? 0x105EC76F0 : 0x1DB710641; // P'(x) << 1

            crc = context.VectorInsert(context.VectorZero(), crc, 0);

            switch (size)
            {
                case 0: data = context.VectorInsert8(context.VectorZero(), data, 0); break;
                case 1: data = context.VectorInsert16(context.VectorZero(), data, 0); break;
                case 2: data = context.VectorInsert(context.VectorZero(), data, 0); break;
            }

            int bitsize = (8 << size);

            Operand tmp = context.AddIntrinsic(Intrinsic.X86Pxor, crc, data);
            tmp = context.AddIntrinsic(Intrinsic.X86Psllq, tmp, Const(64 - bitsize));
            tmp = context.AddIntrinsic(Intrinsic.X86Pclmulqdq, tmp, X86GetScalar(context, mu), Const(0));
            tmp = context.AddIntrinsic(Intrinsic.X86Pclmulqdq, tmp, X86GetScalar(context, polynomial), Const(0));

            if (bitsize < 32)
            {
                crc = context.AddIntrinsic(Intrinsic.X86Pslldq, crc, Const((64 - bitsize) / 8));
                tmp = context.AddIntrinsic(Intrinsic.X86Pxor, tmp, crc);
            }

            return context.VectorExtract(OperandType.I32, tmp, 2);
        }

        private static Operand EmitCrc32Optimized64(ArmEmitterContext context, Operand crc, Operand data, bool castagnoli)
        {
            long mu = castagnoli ? 0x0DEA713F1 : 0x1F7011641; // mu' = floor(x^64/P(x))'
            long polynomial = castagnoli ? 0x105EC76F0 : 0x1DB710641; // P'(x) << 1

            crc = context.VectorInsert(context.VectorZero(), crc, 0);
            data = context.VectorInsert(context.VectorZero(), data, 0);

            Operand tmp = context.AddIntrinsic(Intrinsic.X86Pxor, crc, data);
            Operand res = context.AddIntrinsic(Intrinsic.X86Pslldq, tmp, Const(4));

            tmp = context.AddIntrinsic(Intrinsic.X86Pclmulqdq, res, X86GetScalar(context, mu), Const(0));
            tmp = context.AddIntrinsic(Intrinsic.X86Pclmulqdq, tmp, X86GetScalar(context, polynomial), Const(0));

            tmp = context.AddIntrinsic(Intrinsic.X86Pxor, tmp, res);
            tmp = context.AddIntrinsic(Intrinsic.X86Psllq, tmp, Const(32));

            tmp = context.AddIntrinsic(Intrinsic.X86Pclmulqdq, tmp, X86GetScalar(context, mu), Const(1));
            tmp = context.AddIntrinsic(Intrinsic.X86Pclmulqdq, tmp, X86GetScalar(context, polynomial), Const(0));

            return context.VectorExtract(OperandType.I32, tmp, 2);
        }
    }
}
