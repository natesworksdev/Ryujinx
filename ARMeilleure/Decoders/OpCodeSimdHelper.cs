using System;
using System.Collections.Generic;
using System.Text;

namespace ARMeilleure.Decoders
{
    public static class OpCodeSimdHelper
    {
        public static Tuple<long, int> GetSimdImmediateAndSize(int cMode, int op, long imm, int fpBaseSize = 0)
        {
            int modeLow = cMode & 1;
            int modeHigh = cMode >> 1;
            int size = 0;

            if (modeHigh == 0b111)
            {
                switch (op | (modeLow << 1))
                {
                    case 0:
                        // 64-bits Immediate.
                        // Transform abcd efgh into abcd efgh abcd efgh ...
                        size = 3;
                        imm = (long)((ulong)imm * 0x0101010101010101);
                        break;

                    case 1:
                        // 64-bits Immediate.
                        // Transform abcd efgh into aaaa aaaa bbbb bbbb ...
                        size = 3;
                        imm = (imm & 0xf0) >> 4 | (imm & 0x0f) << 4;
                        imm = (imm & 0xcc) >> 2 | (imm & 0x33) << 2;
                        imm = (imm & 0xaa) >> 1 | (imm & 0x55) << 1;

                        imm = (long)((ulong)imm * 0x8040201008040201);
                        imm = (long)((ulong)imm & 0x8080808080808080);

                        imm |= imm >> 4;
                        imm |= imm >> 2;
                        imm |= imm >> 1;
                        break;

                    case 2:
                        // 2 x 32-bits floating point Immediate.
                        size = 3;
                        imm = (long)DecoderHelper.Imm8ToFP32Table[(int)imm];
                        imm |= imm << 32;
                        break;

                    case 3:
                        // 64-bits floating point Immediate.
                        size = 3;
                        imm = (long)DecoderHelper.Imm8ToFP64Table[(int)imm];
                        break;
                }
            }
            else if ((modeHigh & 0b110) == 0b100)
            {
                // 16-bits shifted Immediate.
                size = 1; imm <<= (modeHigh & 1) << 3;
            }
            else if ((modeHigh & 0b100) == 0b000)
            {
                // 32-bits shifted Immediate.
                size = 2; imm <<= modeHigh << 3;
            }
            else if ((modeHigh & 0b111) == 0b110)
            {
                // 32-bits shifted Immediate (fill with ones).
                size = 2; imm = ShlOnes(imm, 8 << modeLow);
            }
            else
            {
                // 8-bits without shift.
                size = 0;
            }

            return new Tuple<long, int>(imm, size);
        }

        public static long VFPExpandImm(long imm, int n)
        {
            int e = (n == 16) ? 5 : ((n == 32) ? 8 : 11);
            int f = (n) * 8 - e - 1;
            long sign = (imm & 0x80) << (n - 8);

            var bit6 = (imm >> 6) & 0x1;
            
            long exp = ((imm >> 4) & 0x3);
            if (bit6 == 1) exp |= ShlOnes(0, e - 3) << 2;
            if (bit6 == 0) exp |= (long)1 << (e - 1);

            long frac = (imm & 0xf) << (f - 4);

            return sign | (exp << f) | frac;
        }

        private static long ShlOnes(long value, int shift)
        {
            if (shift != 0)
            {
                return value << shift | (long)(ulong.MaxValue >> (64 - shift));
            }
            else
            {
                return value;
            }
        }
    }
}
