using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation;

using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Instructions
{
    static class Lop3Expression
    {
        public static Operand GetFromTruthTable(EmitterContext context, Operand srcA, Operand srcB, Operand srcC, int imm)
        {
            Operand notSrcA = context.BitwiseNot(srcA);
            Operand notSrcB = context.BitwiseNot(srcB);
            Operand notSrcC = context.BitwiseNot(srcC);

            for (int i = 0; i < 0x40; i++)
            {
                int currImm = imm;

                Operand x = srcA;
                Operand y = srcB;
                Operand z = srcC;

                if ((i & 0x01) != 0)
                {
                    x = notSrcA;
                    currImm = PermuteByte(currImm, 4, 3, 2, 1, 7, 6, 5, 4);
                }

                if ((i & 0x02) != 0)
                {
                    y = notSrcB;
                    currImm = PermuteByte(currImm, 5, 4, 7, 6, 1, 0, 3, 2);
                }

                if ((i & 0x04) != 0)
                {
                    z = notSrcC;
                    currImm = PermuteByte(currImm, 6, 7, 4, 5, 2, 3, 1, 0);
                }

                if ((i & 0x08) != 0)
                {
                    (x, y) = (y, x);
                    currImm = PermuteByte(currImm, 7, 6, 3, 2, 5, 4, 1, 0);
                }

                if ((i & 0x10) != 0)
                {
                    (x, z) = (z, x);
                    currImm = PermuteByte(currImm, 7, 3, 5, 1, 6, 2, 4, 0);
                }

                if ((i & 0x20) != 0)
                {
                    (y, z) = (z, y);
                    currImm = PermuteByte(currImm, 7, 5, 6, 4, 3, 1, 2, 0);
                }

                Operand result = GetExpr(currImm, context, x, y, z);
                if (result != null)
                {
                    return result;
                }

                Operand notResult = GetExpr((~currImm) & 0xff, context, x, y, z);
                if (notResult != null)
                {
                    return context.BitwiseNot(notResult);
                }
            }

            return null;
        }

        private static Operand GetExpr(int imm, EmitterContext context, Operand x, Operand y, Operand z)
        {
            return imm switch
            {
                // False
                0x00 => Const(IrConsts.False),
                // True
                0xff => Const(IrConsts.True),
                // In
                0xf0 => x,
                // And2
                0xc0 => context.BitwiseAnd(x, y),
                // Xor2
                0x3c => context.BitwiseExclusiveOr(x, y),
                // And3
                0x80 => context.BitwiseAnd(x, context.BitwiseAnd(y, z)),
                // XorAnd
                0x60 => context.BitwiseAnd(x, context.BitwiseExclusiveOr(y, z)),
                // OrAnd
                0xe0 => context.BitwiseAnd(x, context.BitwiseOr(y, z)),
                // Onehot
                0x16 => context.BitwiseExclusiveOr(context.BitwiseOr(x, y), context.BitwiseAnd(z, context.BitwiseOr(x, y))),
                // Majority
                0xe8 => context.BitwiseAnd(context.BitwiseOr(x, y), context.BitwiseOr(z, context.BitwiseAnd(x, y))),
                // Inverse Gamble
                0x7e => context.BitwiseOr(context.BitwiseExclusiveOr(x, y), context.BitwiseExclusiveOr(x, y)),
                // Dot
                0x1a => context.BitwiseAnd(context.BitwiseExclusiveOr(x, z), context.BitwiseOr(context.BitwiseNot(y), z)),
                // Mux
                0xca => context.BitwiseOr(context.BitwiseAnd(x, y), context.BitwiseAnd(context.BitwiseNot(x), z)),
                // AndXor
                0x78 => context.BitwiseExclusiveOr(x, context.BitwiseAnd(y, z)),
                // Xor3
                0x96 => context.BitwiseExclusiveOr(x, context.BitwiseExclusiveOr(y, z)),
                // Not one of the base cases
                _ => null
            };
        }

        private static int PermuteByte(int imm, int bit7, int bit6, int bit5, int bit4, int bit3, int bit2, int bit1, int bit0)
        {
            int result = 0;

            result |= ((imm >> 0) & 1) << bit0;
            result |= ((imm >> 1) & 1) << bit1;
            result |= ((imm >> 2) & 1) << bit2;
            result |= ((imm >> 3) & 1) << bit3;
            result |= ((imm >> 4) & 1) << bit4;
            result |= ((imm >> 5) & 1) << bit5;
            result |= ((imm >> 6) & 1) << bit6;
            result |= ((imm >> 7) & 1) << bit7;

            return result;
        }
    }
}