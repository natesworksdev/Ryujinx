using ARMeilleure.State;
using System;

namespace ARMeilleure.Instructions
{
    static class SoftFallback
    {
#region "ShlReg"

#endregion

#region "ShrImm64"

#endregion

#region "Rounding"
        public static double Round(double value)
        {
            ExecutionContext context = NativeInterface.GetContext();

            FPRoundingMode roundMode = context.Fpcr.GetRoundingMode();

            if (roundMode == FPRoundingMode.ToNearest)
            {
                return Math.Round(value); // even
            }
            else if (roundMode == FPRoundingMode.TowardsPlusInfinity)
            {
                return Math.Ceiling(value);
            }
            else if (roundMode == FPRoundingMode.TowardsMinusInfinity)
            {
                return Math.Floor(value);
            }
            else /* if (roundMode == FPRoundingMode.TowardsZero) */
            {
                return Math.Truncate(value);
            }
        }

        public static float RoundF(float value)
        {
            ExecutionContext context = NativeInterface.GetContext();

            FPRoundingMode roundMode = context.Fpcr.GetRoundingMode();

            if (roundMode == FPRoundingMode.ToNearest)
            {
                return MathF.Round(value); // even
            }
            else if (roundMode == FPRoundingMode.TowardsPlusInfinity)
            {
                return MathF.Ceiling(value);
            }
            else if (roundMode == FPRoundingMode.TowardsMinusInfinity)
            {
                return MathF.Floor(value);
            }
            else /* if (roundMode == FPRoundingMode.TowardsZero) */
            {
                return MathF.Truncate(value);
            }
        }
#endregion

#region "Saturating"
        public static long SignedSrcSignedDstSatQ(long op, int size)
        {
            ExecutionContext context = NativeInterface.GetContext();

            int eSize = 8 << size;

            long tMaxValue =  (1L << (eSize - 1)) - 1L;
            long tMinValue = -(1L << (eSize - 1));

            if (op > tMaxValue)
            {
                context.Fpsr |= FPSR.Qc;

                return tMaxValue;
            }
            else if (op < tMinValue)
            {
                context.Fpsr |= FPSR.Qc;

                return tMinValue;
            }
            else
            {
                return op;
            }
        }

        public static ulong SignedSrcUnsignedDstSatQ(long op, int size)
        {
            ExecutionContext context = NativeInterface.GetContext();

            int eSize = 8 << size;

            ulong tMaxValue = (1UL << eSize) - 1UL;
            ulong tMinValue =  0UL;

            if (op > (long)tMaxValue)
            {
                context.Fpsr |= FPSR.Qc;

                return tMaxValue;
            }
            else if (op < (long)tMinValue)
            {
                context.Fpsr |= FPSR.Qc;

                return tMinValue;
            }
            else
            {
                return (ulong)op;
            }
        }

        public static long UnsignedSrcSignedDstSatQ(ulong op, int size)
        {
            ExecutionContext context = NativeInterface.GetContext();

            int eSize = 8 << size;

            long tMaxValue = (1L << (eSize - 1)) - 1L;

            if (op > (ulong)tMaxValue)
            {
                context.Fpsr |= FPSR.Qc;

                return tMaxValue;
            }
            else
            {
                return (long)op;
            }
        }

        public static ulong UnsignedSrcUnsignedDstSatQ(ulong op, int size)
        {
            ExecutionContext context = NativeInterface.GetContext();

            int eSize = 8 << size;

            ulong tMaxValue = (1UL << eSize) - 1UL;

            if (op > tMaxValue)
            {
                context.Fpsr |= FPSR.Qc;

                return tMaxValue;
            }
            else
            {
                return op;
            }
        }

        public static long UnarySignedSatQAbsOrNeg(long op)
        {
            ExecutionContext context = NativeInterface.GetContext();

            if (op == long.MinValue)
            {
                context.Fpsr |= FPSR.Qc;

                return long.MaxValue;
            }
            else
            {
                return op;
            }
        }

        public static long BinarySignedSatQAdd(long op1, long op2)
        {
            ExecutionContext context = NativeInterface.GetContext();

            long add = op1 + op2;

            if ((~(op1 ^ op2) & (op1 ^ add)) < 0L)
            {
                context.Fpsr |= FPSR.Qc;

                if (op1 < 0L)
                {
                    return long.MinValue;
                }
                else
                {
                    return long.MaxValue;
                }
            }
            else
            {
                return add;
            }
        }

        public static ulong BinaryUnsignedSatQAdd(ulong op1, ulong op2)
        {
            ExecutionContext context = NativeInterface.GetContext();

            ulong add = op1 + op2;

            if ((add < op1) && (add < op2))
            {
                context.Fpsr |= FPSR.Qc;

                return ulong.MaxValue;
            }
            else
            {
                return add;
            }
        }

        public static long BinarySignedSatQSub(long op1, long op2)
        {
            ExecutionContext context = NativeInterface.GetContext();

            long sub = op1 - op2;

            if (((op1 ^ op2) & (op1 ^ sub)) < 0L)
            {
                context.Fpsr |= FPSR.Qc;

                if (op1 < 0L)
                {
                    return long.MinValue;
                }
                else
                {
                    return long.MaxValue;
                }
            }
            else
            {
                return sub;
            }
        }

        public static ulong BinaryUnsignedSatQSub(ulong op1, ulong op2)
        {
            ExecutionContext context = NativeInterface.GetContext();

            ulong sub = op1 - op2;

            if (op1 < op2)
            {
                context.Fpsr |= FPSR.Qc;

                return ulong.MinValue;
            }
            else
            {
                return sub;
            }
        }

        public static long BinarySignedSatQAcc(ulong op1, long op2)
        {
            ExecutionContext context = NativeInterface.GetContext();

            if (op1 <= (ulong)long.MaxValue)
            {
                // op1 from ulong.MinValue to (ulong)long.MaxValue
                // op2 from long.MinValue to long.MaxValue

                long add = (long)op1 + op2;

                if ((~op2 & add) < 0L)
                {
                    context.Fpsr |= FPSR.Qc;

                    return long.MaxValue;
                }
                else
                {
                    return add;
                }
            }
            else if (op2 >= 0L)
            {
                // op1 from (ulong)long.MaxValue + 1UL to ulong.MaxValue
                // op2 from (long)ulong.MinValue to long.MaxValue

                context.Fpsr |= FPSR.Qc;

                return long.MaxValue;
            }
            else
            {
                // op1 from (ulong)long.MaxValue + 1UL to ulong.MaxValue
                // op2 from long.MinValue to (long)ulong.MinValue - 1L

                ulong add = op1 + (ulong)op2;

                if (add > (ulong)long.MaxValue)
                {
                    context.Fpsr |= FPSR.Qc;

                    return long.MaxValue;
                }
                else
                {
                    return (long)add;
                }
            }
        }

        public static ulong BinaryUnsignedSatQAcc(long op1, ulong op2)
        {
            ExecutionContext context = NativeInterface.GetContext();

            if (op1 >= 0L)
            {
                // op1 from (long)ulong.MinValue to long.MaxValue
                // op2 from ulong.MinValue to ulong.MaxValue

                ulong add = (ulong)op1 + op2;

                if ((add < (ulong)op1) && (add < op2))
                {
                    context.Fpsr |= FPSR.Qc;

                    return ulong.MaxValue;
                }
                else
                {
                    return add;
                }
            }
            else if (op2 > (ulong)long.MaxValue)
            {
                // op1 from long.MinValue to (long)ulong.MinValue - 1L
                // op2 from (ulong)long.MaxValue + 1UL to ulong.MaxValue

                return (ulong)op1 + op2;
            }
            else
            {
                // op1 from long.MinValue to (long)ulong.MinValue - 1L
                // op2 from ulong.MinValue to (ulong)long.MaxValue

                long add = op1 + (long)op2;

                if (add < (long)ulong.MinValue)
                {
                    context.Fpsr |= FPSR.Qc;

                    return ulong.MinValue;
                }
                else
                {
                    return (ulong)add;
                }
            }
        }
#endregion

#region "Count"
        public static ulong CountLeadingSigns(ulong value, int size) // size is 8, 16, 32 or 64 (SIMD&FP or Base Inst.).
        {
            value ^= value >> 1;

            int highBit = size - 2;

            for (int bit = highBit; bit >= 0; bit--)
            {
                if (((int)(value >> bit) & 0b1) != 0)
                {
                    return (ulong)(highBit - bit);
                }
            }

            return (ulong)(size - 1);
        }

        private static readonly byte[] ClzNibbleTbl = { 4, 3, 2, 2, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0 };

        public static ulong CountLeadingZeros(ulong value, int size) // size is 8, 16, 32 or 64 (SIMD&FP or Base Inst.).
        {
            if (value == 0ul)
            {
                return (ulong)size;
            }

            int nibbleIdx = size;
            int preCount, count = 0;

            do
            {
                nibbleIdx -= 4;
                preCount = ClzNibbleTbl[(int)(value >> nibbleIdx) & 0b1111];
                count += preCount;
            }
            while (preCount == 4);

            return (ulong)count;
        }

        public static ulong CountSetBits8(ulong value) // "size" is 8 (SIMD&FP Inst.).
        {
            value = ((value >> 1) & 0x55ul) + (value & 0x55ul);
            value = ((value >> 2) & 0x33ul) + (value & 0x33ul);

            return (value >> 4) + (value & 0x0ful);
        }
#endregion

#region "Crc32"
        private const uint Crc32RevPoly  = 0xedb88320;
        private const uint Crc32cRevPoly = 0x82f63b78;

        public static uint Crc32b(uint crc, byte   value) => Crc32 (crc, Crc32RevPoly, value);
        public static uint Crc32h(uint crc, ushort value) => Crc32h(crc, Crc32RevPoly, value);
        public static uint Crc32w(uint crc, uint   value) => Crc32w(crc, Crc32RevPoly, value);
        public static uint Crc32x(uint crc, ulong  value) => Crc32x(crc, Crc32RevPoly, value);

        public static uint Crc32cb(uint crc, byte   value) => Crc32 (crc, Crc32cRevPoly, value);
        public static uint Crc32ch(uint crc, ushort value) => Crc32h(crc, Crc32cRevPoly, value);
        public static uint Crc32cw(uint crc, uint   value) => Crc32w(crc, Crc32cRevPoly, value);
        public static uint Crc32cx(uint crc, ulong  value) => Crc32x(crc, Crc32cRevPoly, value);

        private static uint Crc32h(uint crc, uint poly, ushort val)
        {
            crc = Crc32(crc, poly, (byte)(val >> 0));
            crc = Crc32(crc, poly, (byte)(val >> 8));

            return crc;
        }

        private static uint Crc32w(uint crc, uint poly, uint val)
        {
            crc = Crc32(crc, poly, (byte)(val >> 0 ));
            crc = Crc32(crc, poly, (byte)(val >> 8 ));
            crc = Crc32(crc, poly, (byte)(val >> 16));
            crc = Crc32(crc, poly, (byte)(val >> 24));

            return crc;
        }

        private static uint Crc32x(uint crc, uint poly, ulong val)
        {
            crc = Crc32(crc, poly, (byte)(val >> 0 ));
            crc = Crc32(crc, poly, (byte)(val >> 8 ));
            crc = Crc32(crc, poly, (byte)(val >> 16));
            crc = Crc32(crc, poly, (byte)(val >> 24));
            crc = Crc32(crc, poly, (byte)(val >> 32));
            crc = Crc32(crc, poly, (byte)(val >> 40));
            crc = Crc32(crc, poly, (byte)(val >> 48));
            crc = Crc32(crc, poly, (byte)(val >> 56));

            return crc;
        }

        private static uint Crc32(uint crc, uint poly, byte val)
        {
            crc ^= val;

            for (int bit = 7; bit >= 0; bit--)
            {
                uint mask = (uint)(-(int)(crc & 1));

                crc = (crc >> 1) ^ (poly & mask);
            }

            return crc;
        }
#endregion

#region "Aes"
        public static V128 Decrypt(V128 value, V128 roundKey)
        {
            return CryptoHelper.AesInvSubBytes(CryptoHelper.AesInvShiftRows(value ^ roundKey));
        }

        public static V128 Encrypt(V128 value, V128 roundKey)
        {
            return CryptoHelper.AesSubBytes(CryptoHelper.AesShiftRows(value ^ roundKey));
        }

        public static V128 InverseMixColumns(V128 value)
        {
            return CryptoHelper.AesInvMixColumns(value);
        }

        public static V128 MixColumns(V128 value)
        {
            return CryptoHelper.AesMixColumns(value);
        }
#endregion

#region "Sha1"

#endregion

#region "Sha256"

#endregion

#region "Reverse"
        public static uint ReverseBits8(uint value)
        {
            value = ((value & 0xaa) >> 1) | ((value & 0x55) << 1);
            value = ((value & 0xcc) >> 2) | ((value & 0x33) << 2);

            return (value >> 4) | ((value & 0x0f) << 4);
        }

        public static uint ReverseBits32(uint value)
        {
            value = ((value & 0xaaaaaaaa) >> 1) | ((value & 0x55555555) << 1);
            value = ((value & 0xcccccccc) >> 2) | ((value & 0x33333333) << 2);
            value = ((value & 0xf0f0f0f0) >> 4) | ((value & 0x0f0f0f0f) << 4);
            value = ((value & 0xff00ff00) >> 8) | ((value & 0x00ff00ff) << 8);

            return (value >> 16) | (value << 16);
        }

        public static ulong ReverseBits64(ulong value)
        {
            value = ((value & 0xaaaaaaaaaaaaaaaa) >> 1 ) | ((value & 0x5555555555555555) << 1 );
            value = ((value & 0xcccccccccccccccc) >> 2 ) | ((value & 0x3333333333333333) << 2 );
            value = ((value & 0xf0f0f0f0f0f0f0f0) >> 4 ) | ((value & 0x0f0f0f0f0f0f0f0f) << 4 );
            value = ((value & 0xff00ff00ff00ff00) >> 8 ) | ((value & 0x00ff00ff00ff00ff) << 8 );
            value = ((value & 0xffff0000ffff0000) >> 16) | ((value & 0x0000ffff0000ffff) << 16);

            return (value >> 32) | (value << 32);
        }

        public static uint ReverseBytes16_32(uint value) => (uint)ReverseBytes16_64(value);

        public static ulong ReverseBytes16_64(ulong value) => ReverseBytes(value, RevSize.Rev16);
        public static ulong ReverseBytes32_64(ulong value) => ReverseBytes(value, RevSize.Rev32);

        private enum RevSize
        {
            Rev16,
            Rev32,
            Rev64
        }

        private static ulong ReverseBytes(ulong value, RevSize size)
        {
            value = ((value & 0xff00ff00ff00ff00) >> 8) | ((value & 0x00ff00ff00ff00ff) << 8);

            if (size == RevSize.Rev16)
            {
                return value;
            }

            value = ((value & 0xffff0000ffff0000) >> 16) | ((value & 0x0000ffff0000ffff) << 16);

            if (size == RevSize.Rev32)
            {
                return value;
            }

            value = ((value & 0xffffffff00000000) >> 32) | ((value & 0x00000000ffffffff) << 32);

            if (size == RevSize.Rev64)
            {
                return value;
            }

            throw new ArgumentException(nameof(size));
        }
#endregion
    }
}
