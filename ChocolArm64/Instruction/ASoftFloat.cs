using ChocolArm64.State;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ChocolArm64.Instruction
{
    static class ASoftFloat
    {
        static ASoftFloat()
        {
            RecipEstimateTable   = BuildRecipEstimateTable();
            InvSqrtEstimateTable = BuildInvSqrtEstimateTable();
        }

        private static readonly byte[] RecipEstimateTable;
        private static readonly byte[] InvSqrtEstimateTable;

        private static byte[] BuildRecipEstimateTable()
        {
            byte[] table = new byte[256];
            for (ulong index = 0; index < 256; index++)
            {
                ulong a = index | 0x100;

                a = (a << 1) + 1;
                ulong b = 0x80000 / a;
                b = (b + 1) >> 1;

                table[index] = (byte)(b & 0xFF);
            }
            return table;
        }

        private static byte[] BuildInvSqrtEstimateTable()
        {
            byte[] table = new byte[512];
            for (ulong index = 128; index < 512; index++)
            {
                ulong a = index;
                if (a < 256)
                {
                    a = (a << 1) + 1;
                }
                else
                {
                    a = (a | 1) << 1;
                }

                ulong b = 256;
                while (a * (b + 1) * (b + 1) < (1ul << 28))
                {
                    b++;
                }
                b = (b + 1) >> 1;

                table[index] = (byte)(b & 0xFF);
            }
            return table;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RecipEstimate(float x)
        {
            return (float)RecipEstimate((double)x);
        }

        public static double RecipEstimate(double x)
        {
            ulong xBits = (ulong)BitConverter.DoubleToInt64Bits(x);
            ulong xSign = xBits & 0x8000000000000000;
            ulong xExp = (xBits >> 52) & 0x7FF;
            ulong scaled = xBits & ((1ul << 52) - 1);

            if (xExp >= 2045)
            {
                if (xExp == 0x7ff && scaled != 0)
                {
                    // NaN
                    return BitConverter.Int64BitsToDouble((long)(xBits | 0x0008000000000000));
                }

                // Infinity, or Out of range -> Zero
                return BitConverter.Int64BitsToDouble((long)xSign);
            }

            if (xExp == 0)
            {
                if (scaled == 0)
                {
                    // Zero -> Infinity
                    return BitConverter.Int64BitsToDouble((long)(xSign | 0x7FF0000000000000));
                }

                // Denormal
                if ((scaled & (1ul << 51)) == 0)
                {
                    xExp = ~0ul;
                    scaled <<= 2;
                }
                else
                {
                    scaled <<= 1;
                }
            }

            scaled >>= 44;
            scaled &= 0xFF;

            ulong resultExp = (2045 - xExp) & 0x7FF;
            ulong estimate = (ulong)RecipEstimateTable[scaled];
            ulong fraction = estimate << 44;

            if (resultExp == 0)
            {
                fraction >>= 1;
                fraction |= 1ul << 51;
            }
            else if (resultExp == 0x7FF)
            {
                resultExp = 0;
                fraction >>= 2;
                fraction |= 1ul << 50;
            }

            ulong result = xSign | (resultExp << 52) | fraction;
            return BitConverter.Int64BitsToDouble((long)result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InvSqrtEstimate(float x)
        {
            return (float)InvSqrtEstimate((double)x);
        }

        public static double InvSqrtEstimate(double x)
        {
            ulong xBits = (ulong)BitConverter.DoubleToInt64Bits(x);
            ulong xSign = xBits & 0x8000000000000000;
            long xExp = (long)((xBits >> 52) & 0x7FF);
            ulong scaled = xBits & ((1ul << 52) - 1);

            if (xExp == 0x7FF && scaled != 0)
            {
                // NaN
                return BitConverter.Int64BitsToDouble((long)(xBits | 0x0008000000000000));
            }

            if (xExp == 0)
            {
                if (scaled == 0)
                {
                    // Zero -> Infinity
                    return BitConverter.Int64BitsToDouble((long)(xSign | 0x7FF0000000000000));
                }

                // Denormal
                while ((scaled & (1 << 51)) == 0)
                {
                    scaled <<= 1;
                    xExp--;
                }
                scaled <<= 1;
            }

            if (xSign != 0)
            {
                // Negative -> NaN
                return BitConverter.Int64BitsToDouble((long)0x7FF8000000000000);
            }

            if (xExp == 0x7ff && scaled == 0)
            {
                // Infinity -> Zero
                return BitConverter.Int64BitsToDouble((long)xSign);
            }

            if (((ulong)xExp & 1) == 1)
            {
                scaled >>= 45;
                scaled &= 0xFF;
                scaled |= 0x80;
            }
            else
            {
                scaled >>= 44;
                scaled &= 0xFF;
                scaled |= 0x100;
            }

            ulong resultExp = ((ulong)(3068 - xExp) / 2) & 0x7FF;
            ulong estimate = (ulong)InvSqrtEstimateTable[scaled];
            ulong fraction = estimate << 44;

            ulong result = xSign | (resultExp << 52) | fraction;
            return BitConverter.Int64BitsToDouble((long)result);
        }
    }

    static class ASoftFloat1632
    {
        public static float FpConvert(ushort valueBits, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat16_32.FPConvert: State.Fpcr = 0x{state.Fpcr:X8}");

            double real = valueBits.FpUnpackCv(out FpType type, out bool sign, state);

            float result;

            if (type == FpType.SnaN || type == FpType.QnaN)
            {
                if (state.GetFpcrFlag(Fpcr.Dn))
                {
                    result = FpDefaultNaN();
                }
                else
                {
                    result = FpConvertNaN(valueBits);
                }

                if (type == FpType.SnaN)
                {
                    FpProcessException(FpExc.InvalidOp, state);
                }
            }
            else if (type == FpType.Infinity)
            {
                result = FpInfinity(sign);
            }
            else if (type == FpType.Zero)
            {
                result = FpZero(sign);
            }
            else
            {
                result = FpRoundCv(real, state);
            }

            return result;
        }

        private static float FpDefaultNaN()
        {
            return -float.NaN;
        }

        private static float FpInfinity(bool sign)
        {
            return sign ? float.NegativeInfinity : float.PositiveInfinity;
        }

        private static float FpZero(bool sign)
        {
            return sign ? -0f : +0f;
        }

        private static float FpMaxNormal(bool sign)
        {
            return sign ? float.MinValue : float.MaxValue;
        }

        private static double FpUnpackCv(this ushort valueBits, out FpType type, out bool sign, AThreadState state)
        {
            sign = (~(uint)valueBits & 0x8000u) == 0u;

            uint exp16  = ((uint)valueBits & 0x7C00u) >> 10;
            uint frac16 =  (uint)valueBits & 0x03FFu;

            double real;

            if (exp16 == 0u)
            {
                if (frac16 == 0u)
                {
                    type = FpType.Zero;
                    real = 0d;
                }
                else
                {
                    type = FpType.Nonzero; // Subnormal.
                    real = Math.Pow(2d, -14) * ((double)frac16 * Math.Pow(2d, -10));
                }
            }
            else if (exp16 == 0x1Fu && !state.GetFpcrFlag(Fpcr.Ahp))
            {
                if (frac16 == 0u)
                {
                    type = FpType.Infinity;
                    real = Math.Pow(2d, 1000);
                }
                else
                {
                    type = (~frac16 & 0x0200u) == 0u ? FpType.QnaN : FpType.SnaN;
                    real = 0d;
                }
            }
            else
            {
                type = FpType.Nonzero; // Normal.
                real = Math.Pow(2d, (int)exp16 - 15) * (1d + (double)frac16 * Math.Pow(2d, -10));
            }

            return sign ? -real : real;
        }

        private static float FpRoundCv(double real, AThreadState state)
        {
            const int minimumExp = -126;

            const int e = 8;
            const int f = 23;

            bool   sign;
            double mantissa;

            if (real < 0d)
            {
                sign     = true;
                mantissa = -real;
            }
            else
            {
                sign     = false;
                mantissa = real;
            }

            int exponent = 0;

            while (mantissa < 1d)
            {
                mantissa *= 2d;
                exponent--;
            }

            while (mantissa >= 2d)
            {
                mantissa /= 2d;
                exponent++;
            }

            if (state.GetFpcrFlag(Fpcr.Fz) && exponent < minimumExp)
            {
                state.SetFpsrFlag(Fpsr.Ufc);

                return FpZero(sign);
            }

            uint biasedExp = (uint)Math.Max(exponent - minimumExp + 1, 0);

            if (biasedExp == 0u)
            {
                mantissa /= Math.Pow(2d, minimumExp - exponent);
            }

            uint intMant = (uint)Math.Floor(mantissa * Math.Pow(2d, f));
            double error = mantissa * Math.Pow(2d, f) - (double)intMant;

            if (biasedExp == 0u && (error != 0d || state.GetFpcrFlag(Fpcr.Ufe)))
            {
                FpProcessException(FpExc.Underflow, state);
            }

            bool overflowToInf;
            bool roundUp;

            switch (state.FpRoundingMode())
            {
                default:
                case ARoundMode.ToNearest:
                    roundUp       = (error > 0.5d || (error == 0.5d && (intMant & 1u) == 1u));
                    overflowToInf = true;
                    break;

                case ARoundMode.TowardsPlusInfinity:
                    roundUp       = (error != 0d && !sign);
                    overflowToInf = !sign;
                    break;

                case ARoundMode.TowardsMinusInfinity:
                    roundUp       = (error != 0d && sign);
                    overflowToInf = sign;
                    break;

                case ARoundMode.TowardsZero:
                    roundUp       = false;
                    overflowToInf = false;
                    break;
            }

            if (roundUp)
            {
                intMant++;

                if (intMant == (uint)Math.Pow(2d, f))
                {
                    biasedExp = 1u;
                }

                if (intMant == (uint)Math.Pow(2d, f + 1))
                {
                    biasedExp++;
                    intMant >>= 1;
                }
            }

            float result;

            if (biasedExp >= (uint)Math.Pow(2d, e) - 1u)
            {
                result = overflowToInf ? FpInfinity(sign) : FpMaxNormal(sign);

                FpProcessException(FpExc.Overflow, state);

                error = 1d;
            }
            else
            {
                result = BitConverter.Int32BitsToSingle(
                    (int)((sign ? 1u : 0u) << 31 | (biasedExp & 0xFFu) << 23 | (intMant & 0x007FFFFFu)));
            }

            if (error != 0d)
            {
                FpProcessException(FpExc.Inexact, state);
            }

            return result;
        }

        private static float FpConvertNaN(ushort valueBits)
        {
            return BitConverter.Int32BitsToSingle(
                (int)(((uint)valueBits & 0x8000u) << 16 | 0x7FC00000u | ((uint)valueBits & 0x01FFu) << 13));
        }

        private static void FpProcessException(FpExc exc, AThreadState state)
        {
            int enable = (int)exc + 8;

            if ((state.Fpcr & (1 << enable)) != 0)
            {
                throw new NotImplementedException("floating-point trap handling");
            }
            else
            {
                state.Fpsr |= 1 << (int)exc;
            }
        }
    }

    static class ASoftFloat3216
    {
        public static ushort FpConvert(float value, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat32_16.FPConvert: State.Fpcr = 0x{state.Fpcr:X8}");

            double real = value.FpUnpackCv(out FpType type, out bool sign, state, out uint valueBits);

            bool altHp = state.GetFpcrFlag(Fpcr.Ahp);

            ushort resultBits;

            if (type == FpType.SnaN || type == FpType.QnaN)
            {
                if (altHp)
                {
                    resultBits = FpZero(sign);
                }
                else if (state.GetFpcrFlag(Fpcr.Dn))
                {
                    resultBits = FpDefaultNaN();
                }
                else
                {
                    resultBits = FpConvertNaN(valueBits);
                }

                if (type == FpType.SnaN || altHp)
                {
                    FpProcessException(FpExc.InvalidOp, state);
                }
            }
            else if (type == FpType.Infinity)
            {
                if (altHp)
                {
                    resultBits = (ushort)((sign ? 1u : 0u) << 15 | 0x7FFFu);

                    FpProcessException(FpExc.InvalidOp, state);
                }
                else
                {
                    resultBits = FpInfinity(sign);
                }
            }
            else if (type == FpType.Zero)
            {
                resultBits = FpZero(sign);
            }
            else
            {
                resultBits = FpRoundCv(real, state);
            }

            return resultBits;
        }

        private static ushort FpDefaultNaN()
        {
            return (ushort)0x7E00u;
        }

        private static ushort FpInfinity(bool sign)
        {
            return sign ? (ushort)0xFC00u : (ushort)0x7C00u;
        }

        private static ushort FpZero(bool sign)
        {
            return sign ? (ushort)0x8000u : (ushort)0x0000u;
        }

        private static ushort FpMaxNormal(bool sign)
        {
            return sign ? (ushort)0xFBFFu : (ushort)0x7BFFu;
        }

        private static double FpUnpackCv(this float value, out FpType type, out bool sign, AThreadState state, out uint valueBits)
        {
            valueBits = (uint)BitConverter.SingleToInt32Bits(value);

            sign = (~valueBits & 0x80000000u) == 0u;

            uint exp32  = (valueBits & 0x7F800000u) >> 23;
            uint frac32 =  valueBits & 0x007FFFFFu;

            double real;

            if (exp32 == 0u)
            {
                if (frac32 == 0u || state.GetFpcrFlag(Fpcr.Fz))
                {
                    type = FpType.Zero;
                    real = 0d;

                    if (frac32 != 0u) FpProcessException(FpExc.InputDenorm, state);
                }
                else
                {
                    type = FpType.Nonzero; // Subnormal.
                    real = Math.Pow(2d, -126) * ((double)frac32 * Math.Pow(2d, -23));
                }
            }
            else if (exp32 == 0xFFu)
            {
                if (frac32 == 0u)
                {
                    type = FpType.Infinity;
                    real = Math.Pow(2d, 1000);
                }
                else
                {
                    type = (~frac32 & 0x00400000u) == 0u ? FpType.QnaN : FpType.SnaN;
                    real = 0d;
                }
            }
            else
            {
                type = FpType.Nonzero; // Normal.
                real = Math.Pow(2d, (int)exp32 - 127) * (1d + (double)frac32 * Math.Pow(2d, -23));
            }

            return sign ? -real : real;
        }

        private static ushort FpRoundCv(double real, AThreadState state)
        {
            const int minimumExp = -14;

            const int e = 5;
            const int f = 10;

            bool   sign;
            double mantissa;

            if (real < 0d)
            {
                sign     = true;
                mantissa = -real;
            }
            else
            {
                sign     = false;
                mantissa = real;
            }

            int exponent = 0;

            while (mantissa < 1d)
            {
                mantissa *= 2d;
                exponent--;
            }

            while (mantissa >= 2d)
            {
                mantissa /= 2d;
                exponent++;
            }

            uint biasedExp = (uint)Math.Max(exponent - minimumExp + 1, 0);

            if (biasedExp == 0u)
            {
                mantissa /= Math.Pow(2d, minimumExp - exponent);
            }

            uint intMant = (uint)Math.Floor(mantissa * Math.Pow(2d, f));
            double error = mantissa * Math.Pow(2d, f) - (double)intMant;

            if (biasedExp == 0u && (error != 0d || state.GetFpcrFlag(Fpcr.Ufe)))
            {
                FpProcessException(FpExc.Underflow, state);
            }

            bool overflowToInf;
            bool roundUp;

            switch (state.FpRoundingMode())
            {
                default:
                case ARoundMode.ToNearest:
                    roundUp       = (error > 0.5d || (error == 0.5d && (intMant & 1u) == 1u));
                    overflowToInf = true;
                    break;

                case ARoundMode.TowardsPlusInfinity:
                    roundUp       = (error != 0d && !sign);
                    overflowToInf = !sign;
                    break;

                case ARoundMode.TowardsMinusInfinity:
                    roundUp       = (error != 0d && sign);
                    overflowToInf = sign;
                    break;

                case ARoundMode.TowardsZero:
                    roundUp       = false;
                    overflowToInf = false;
                    break;
            }

            if (roundUp)
            {
                intMant++;

                if (intMant == (uint)Math.Pow(2d, f))
                {
                    biasedExp = 1u;
                }

                if (intMant == (uint)Math.Pow(2d, f + 1))
                {
                    biasedExp++;
                    intMant >>= 1;
                }
            }

            ushort resultBits;

            if (!state.GetFpcrFlag(Fpcr.Ahp))
            {
                if (biasedExp >= (uint)Math.Pow(2d, e) - 1u)
                {
                    resultBits = overflowToInf ? FpInfinity(sign) : FpMaxNormal(sign);

                    FpProcessException(FpExc.Overflow, state);

                    error = 1d;
                }
                else
                {
                    resultBits = (ushort)((sign ? 1u : 0u) << 15 | (biasedExp & 0x1Fu) << 10 | (intMant & 0x03FFu));
                }
            }
            else
            {
                if (biasedExp >= (uint)Math.Pow(2d, e))
                {
                    resultBits = (ushort)((sign ? 1u : 0u) << 15 | 0x7FFFu);

                    FpProcessException(FpExc.InvalidOp, state);

                    error = 0d;
                }
                else
                {
                    resultBits = (ushort)((sign ? 1u : 0u) << 15 | (biasedExp & 0x1Fu) << 10 | (intMant & 0x03FFu));
                }
            }

            if (error != 0d)
            {
                FpProcessException(FpExc.Inexact, state);
            }

            return resultBits;
        }

        private static ushort FpConvertNaN(uint valueBits)
        {
            return (ushort)((valueBits & 0x80000000u) >> 16 | 0x7E00u | (valueBits & 0x003FE000u) >> 13);
        }

        private static void FpProcessException(FpExc exc, AThreadState state)
        {
            int enable = (int)exc + 8;

            if ((state.Fpcr & (1 << enable)) != 0)
            {
                throw new NotImplementedException("floating-point trap handling");
            }
            else
            {
                state.Fpsr |= 1 << (int)exc;
            }
        }
    }

    static class ASoftFloat32
    {
        public static float FpAdd(float value1, float value2, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_32.FPAdd: State.Fpcr = 0x{state.Fpcr:X8}");

            value1 = value1.FpUnpack(out FpType type1, out bool sign1, out uint op1);
            value2 = value2.FpUnpack(out FpType type2, out bool sign2, out uint op2);

            float result = FpProcessNaNs(type1, type2, op1, op2, state, out bool done);

            if (!done)
            {
                bool inf1 = type1 == FpType.Infinity; bool zero1 = type1 == FpType.Zero;
                bool inf2 = type2 == FpType.Infinity; bool zero2 = type2 == FpType.Zero;

                if (inf1 && inf2 && sign1 == !sign2)
                {
                    result = FpDefaultNaN();

                    FpProcessException(FpExc.InvalidOp, state);
                }
                else if ((inf1 && !sign1) || (inf2 && !sign2))
                {
                    result = FpInfinity(false);
                }
                else if ((inf1 && sign1) || (inf2 && sign2))
                {
                    result = FpInfinity(true);
                }
                else if (zero1 && zero2 && sign1 == sign2)
                {
                    result = FpZero(sign1);
                }
                else
                {
                    result = value1 + value2;
                }
            }

            return result;
        }

        public static float FpDiv(float value1, float value2, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_32.FPDiv: State.Fpcr = 0x{state.Fpcr:X8}");

            value1 = value1.FpUnpack(out FpType type1, out bool sign1, out uint op1);
            value2 = value2.FpUnpack(out FpType type2, out bool sign2, out uint op2);

            float result = FpProcessNaNs(type1, type2, op1, op2, state, out bool done);

            if (!done)
            {
                bool inf1 = type1 == FpType.Infinity; bool zero1 = type1 == FpType.Zero;
                bool inf2 = type2 == FpType.Infinity; bool zero2 = type2 == FpType.Zero;

                if ((inf1 && inf2) || (zero1 && zero2))
                {
                    result = FpDefaultNaN();

                    FpProcessException(FpExc.InvalidOp, state);
                }
                else if (inf1 || zero2)
                {
                    result = FpInfinity(sign1 ^ sign2);

                    if (!inf1) FpProcessException(FpExc.DivideByZero, state);
                }
                else if (zero1 || inf2)
                {
                    result = FpZero(sign1 ^ sign2);
                }
                else
                {
                    result = value1 / value2;
                }
            }

            return result;
        }

        public static float FpMax(float value1, float value2, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_32.FPMax: State.Fpcr = 0x{state.Fpcr:X8}");

            value1 = value1.FpUnpack(out FpType type1, out bool sign1, out uint op1);
            value2 = value2.FpUnpack(out FpType type2, out bool sign2, out uint op2);

            float result = FpProcessNaNs(type1, type2, op1, op2, state, out bool done);

            if (!done)
            {
                if (value1 > value2)
                {
                    if (type1 == FpType.Infinity)
                    {
                        result = FpInfinity(sign1);
                    }
                    else if (type1 == FpType.Zero)
                    {
                        result = FpZero(sign1 && sign2);
                    }
                    else
                    {
                        result = value1;
                    }
                }
                else
                {
                    if (type2 == FpType.Infinity)
                    {
                        result = FpInfinity(sign2);
                    }
                    else if (type2 == FpType.Zero)
                    {
                        result = FpZero(sign1 && sign2);
                    }
                    else
                    {
                        result = value2;
                    }
                }
            }

            return result;
        }

        public static float FpMaxNum(float value1, float value2, AThreadState state)
        {
            Debug.WriteIf(state.Fpcr != 0, "ASoftFloat_32.FPMaxNum: ");

            value1.FpUnpack(out FpType type1, out _, out _);
            value2.FpUnpack(out FpType type2, out _, out _);

            if (type1 == FpType.QnaN && type2 != FpType.QnaN)
            {
                value1 = FpInfinity(true);
            }
            else if (type1 != FpType.QnaN && type2 == FpType.QnaN)
            {
                value2 = FpInfinity(true);
            }

            return FpMax(value1, value2, state);
        }

        public static float FpMin(float value1, float value2, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_32.FPMin: State.Fpcr = 0x{state.Fpcr:X8}");

            value1 = value1.FpUnpack(out FpType type1, out bool sign1, out uint op1);
            value2 = value2.FpUnpack(out FpType type2, out bool sign2, out uint op2);

            float result = FpProcessNaNs(type1, type2, op1, op2, state, out bool done);

            if (!done)
            {
                if (value1 < value2)
                {
                    if (type1 == FpType.Infinity)
                    {
                        result = FpInfinity(sign1);
                    }
                    else if (type1 == FpType.Zero)
                    {
                        result = FpZero(sign1 || sign2);
                    }
                    else
                    {
                        result = value1;
                    }
                }
                else
                {
                    if (type2 == FpType.Infinity)
                    {
                        result = FpInfinity(sign2);
                    }
                    else if (type2 == FpType.Zero)
                    {
                        result = FpZero(sign1 || sign2);
                    }
                    else
                    {
                        result = value2;
                    }
                }
            }

            return result;
        }

        public static float FpMinNum(float value1, float value2, AThreadState state)
        {
            Debug.WriteIf(state.Fpcr != 0, "ASoftFloat_32.FPMinNum: ");

            value1.FpUnpack(out FpType type1, out _, out _);
            value2.FpUnpack(out FpType type2, out _, out _);

            if (type1 == FpType.QnaN && type2 != FpType.QnaN)
            {
                value1 = FpInfinity(false);
            }
            else if (type1 != FpType.QnaN && type2 == FpType.QnaN)
            {
                value2 = FpInfinity(false);
            }

            return FpMin(value1, value2, state);
        }

        public static float FpMul(float value1, float value2, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_32.FPMul: State.Fpcr = 0x{state.Fpcr:X8}");

            value1 = value1.FpUnpack(out FpType type1, out bool sign1, out uint op1);
            value2 = value2.FpUnpack(out FpType type2, out bool sign2, out uint op2);

            float result = FpProcessNaNs(type1, type2, op1, op2, state, out bool done);

            if (!done)
            {
                bool inf1 = type1 == FpType.Infinity; bool zero1 = type1 == FpType.Zero;
                bool inf2 = type2 == FpType.Infinity; bool zero2 = type2 == FpType.Zero;

                if ((inf1 && zero2) || (zero1 && inf2))
                {
                    result = FpDefaultNaN();

                    FpProcessException(FpExc.InvalidOp, state);
                }
                else if (inf1 || inf2)
                {
                    result = FpInfinity(sign1 ^ sign2);
                }
                else if (zero1 || zero2)
                {
                    result = FpZero(sign1 ^ sign2);
                }
                else
                {
                    result = value1 * value2;
                }
            }

            return result;
        }

        public static float FpMulAdd(float valueA, float value1, float value2, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_32.FPMulAdd: State.Fpcr = 0x{state.Fpcr:X8}");

            valueA = valueA.FpUnpack(out FpType typeA, out bool signA, out uint addend);
            value1 = value1.FpUnpack(out FpType type1, out bool sign1, out uint op1);
            value2 = value2.FpUnpack(out FpType type2, out bool sign2, out uint op2);

            bool inf1 = type1 == FpType.Infinity; bool zero1 = type1 == FpType.Zero;
            bool inf2 = type2 == FpType.Infinity; bool zero2 = type2 == FpType.Zero;

            float result = FpProcessNaNs3(typeA, type1, type2, addend, op1, op2, state, out bool done);

            if (typeA == FpType.QnaN && ((inf1 && zero2) || (zero1 && inf2)))
            {
                result = FpDefaultNaN();

                FpProcessException(FpExc.InvalidOp, state);
            }

            if (!done)
            {
                bool infA = typeA == FpType.Infinity; bool zeroA = typeA == FpType.Zero;

                bool signP = sign1 ^  sign2;
                bool infP  = inf1  || inf2;
                bool zeroP = zero1 || zero2;

                if ((inf1 && zero2) || (zero1 && inf2) || (infA && infP && signA != signP))
                {
                    result = FpDefaultNaN();

                    FpProcessException(FpExc.InvalidOp, state);
                }
                else if ((infA && !signA) || (infP && !signP))
                {
                    result = FpInfinity(false);
                }
                else if ((infA && signA) || (infP && signP))
                {
                    result = FpInfinity(true);
                }
                else if (zeroA && zeroP && signA == signP)
                {
                    result = FpZero(signA);
                }
                else
                {
                    // TODO: When available, use: T MathF.FusedMultiplyAdd(T, T, T);
                    // https://github.com/dotnet/corefx/issues/31903

                    result = valueA + (value1 * value2);
                }
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float FpMulSub(float valueA, float value1, float value2, AThreadState state)
        {
            Debug.WriteIf(state.Fpcr != 0, "ASoftFloat_32.FPMulSub: ");

            value1 = value1.FpNeg();

            return FpMulAdd(valueA, value1, value2, state);
        }

        public static float FpMulX(float value1, float value2, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_32.FPMulX: State.Fpcr = 0x{state.Fpcr:X8}");

            value1 = value1.FpUnpack(out FpType type1, out bool sign1, out uint op1);
            value2 = value2.FpUnpack(out FpType type2, out bool sign2, out uint op2);

            float result = FpProcessNaNs(type1, type2, op1, op2, state, out bool done);

            if (!done)
            {
                bool inf1 = type1 == FpType.Infinity; bool zero1 = type1 == FpType.Zero;
                bool inf2 = type2 == FpType.Infinity; bool zero2 = type2 == FpType.Zero;

                if ((inf1 && zero2) || (zero1 && inf2))
                {
                    result = FpTwo(sign1 ^ sign2);
                }
                else if (inf1 || inf2)
                {
                    result = FpInfinity(sign1 ^ sign2);
                }
                else if (zero1 || zero2)
                {
                    result = FpZero(sign1 ^ sign2);
                }
                else
                {
                    result = value1 * value2;
                }
            }

            return result;
        }

        public static float FpRecipStepFused(float value1, float value2, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_32.FPRecipStepFused: State.Fpcr = 0x{state.Fpcr:X8}");

            value1 = value1.FpNeg();

            value1 = value1.FpUnpack(out FpType type1, out bool sign1, out uint op1);
            value2 = value2.FpUnpack(out FpType type2, out bool sign2, out uint op2);

            float result = FpProcessNaNs(type1, type2, op1, op2, state, out bool done);

            if (!done)
            {
                bool inf1 = type1 == FpType.Infinity; bool zero1 = type1 == FpType.Zero;
                bool inf2 = type2 == FpType.Infinity; bool zero2 = type2 == FpType.Zero;

                if ((inf1 && zero2) || (zero1 && inf2))
                {
                    result = FpTwo(false);
                }
                else if (inf1 || inf2)
                {
                    result = FpInfinity(sign1 ^ sign2);
                }
                else
                {
                    // TODO: When available, use: T MathF.FusedMultiplyAdd(T, T, T);
                    // https://github.com/dotnet/corefx/issues/31903

                    result = 2f + (value1 * value2);
                }
            }

            return result;
        }

        public static float FpRecpX(float value, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_32.FPRecpX: State.Fpcr = 0x{state.Fpcr:X8}");

            value.FpUnpack(out FpType type, out bool sign, out uint op);

            float result;

            if (type == FpType.SnaN || type == FpType.QnaN)
            {
                result = FpProcessNaN(type, op, state);
            }
            else
            {
                uint notExp = (~op >> 23) & 0xFFu;
                uint maxExp = 0xFEu;

                result = BitConverter.Int32BitsToSingle(
                    (int)((sign ? 1u : 0u) << 31 | (notExp == 0xFFu ? maxExp : notExp) << 23));
            }

            return result;
        }

        public static float FprSqrtStepFused(float value1, float value2, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_32.FPRSqrtStepFused: State.Fpcr = 0x{state.Fpcr:X8}");

            value1 = value1.FpNeg();

            value1 = value1.FpUnpack(out FpType type1, out bool sign1, out uint op1);
            value2 = value2.FpUnpack(out FpType type2, out bool sign2, out uint op2);

            float result = FpProcessNaNs(type1, type2, op1, op2, state, out bool done);

            if (!done)
            {
                bool inf1 = type1 == FpType.Infinity; bool zero1 = type1 == FpType.Zero;
                bool inf2 = type2 == FpType.Infinity; bool zero2 = type2 == FpType.Zero;

                if ((inf1 && zero2) || (zero1 && inf2))
                {
                    result = FpOnePointFive(false);
                }
                else if (inf1 || inf2)
                {
                    result = FpInfinity(sign1 ^ sign2);
                }
                else
                {
                    // TODO: When available, use: T MathF.FusedMultiplyAdd(T, T, T);
                    // https://github.com/dotnet/corefx/issues/31903

                    result = (3f + (value1 * value2)) / 2f;
                }
            }

            return result;
        }

        public static float FpSqrt(float value, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_32.FPSqrt: State.Fpcr = 0x{state.Fpcr:X8}");

            value = value.FpUnpack(out FpType type, out bool sign, out uint op);

            float result;

            if (type == FpType.SnaN || type == FpType.QnaN)
            {
                result = FpProcessNaN(type, op, state);
            }
            else if (type == FpType.Zero)
            {
                result = FpZero(sign);
            }
            else if (type == FpType.Infinity && !sign)
            {
                result = FpInfinity(sign);
            }
            else if (sign)
            {
                result = FpDefaultNaN();

                FpProcessException(FpExc.InvalidOp, state);
            }
            else
            {
                result = MathF.Sqrt(value);
            }

            return result;
        }

        public static float FpSub(float value1, float value2, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_32.FPSub: State.Fpcr = 0x{state.Fpcr:X8}");

            value1 = value1.FpUnpack(out FpType type1, out bool sign1, out uint op1);
            value2 = value2.FpUnpack(out FpType type2, out bool sign2, out uint op2);

            float result = FpProcessNaNs(type1, type2, op1, op2, state, out bool done);

            if (!done)
            {
                bool inf1 = type1 == FpType.Infinity; bool zero1 = type1 == FpType.Zero;
                bool inf2 = type2 == FpType.Infinity; bool zero2 = type2 == FpType.Zero;

                if (inf1 && inf2 && sign1 == sign2)
                {
                    result = FpDefaultNaN();

                    FpProcessException(FpExc.InvalidOp, state);
                }
                else if ((inf1 && !sign1) || (inf2 && sign2))
                {
                    result = FpInfinity(false);
                }
                else if ((inf1 && sign1) || (inf2 && !sign2))
                {
                    result = FpInfinity(true);
                }
                else if (zero1 && zero2 && sign1 == !sign2)
                {
                    result = FpZero(sign1);
                }
                else
                {
                    result = value1 - value2;
                }
            }

            return result;
        }

        private static float FpDefaultNaN()
        {
            return -float.NaN;
        }

        private static float FpInfinity(bool sign)
        {
            return sign ? float.NegativeInfinity : float.PositiveInfinity;
        }

        private static float FpZero(bool sign)
        {
            return sign ? -0f : +0f;
        }

        private static float FpTwo(bool sign)
        {
            return sign ? -2f : +2f;
        }

        private static float FpOnePointFive(bool sign)
        {
            return sign ? -1.5f : +1.5f;
        }

        private static float FpNeg(this float value)
        {
            return -value;
        }

        private static float FpUnpack(this float value, out FpType type, out bool sign, out uint valueBits)
        {
            valueBits = (uint)BitConverter.SingleToInt32Bits(value);

            sign = (~valueBits & 0x80000000u) == 0u;

            if ((valueBits & 0x7F800000u) == 0u)
            {
                if ((valueBits & 0x007FFFFFu) == 0u)
                {
                    type = FpType.Zero;
                }
                else
                {
                    type = FpType.Nonzero;
                }
            }
            else if ((~valueBits & 0x7F800000u) == 0u)
            {
                if ((valueBits & 0x007FFFFFu) == 0u)
                {
                    type = FpType.Infinity;
                }
                else
                {
                    type = (~valueBits & 0x00400000u) == 0u
                        ? FpType.QnaN
                        : FpType.SnaN;

                    return FpZero(sign);
                }
            }
            else
            {
                type = FpType.Nonzero;
            }

            return value;
        }

        private static float FpProcessNaNs(
            FpType type1,
            FpType type2,
            uint op1,
            uint op2,
            AThreadState state,
            out bool done)
        {
            done = true;

            if (type1 == FpType.SnaN)
            {
                return FpProcessNaN(type1, op1, state);
            }
            else if (type2 == FpType.SnaN)
            {
                return FpProcessNaN(type2, op2, state);
            }
            else if (type1 == FpType.QnaN)
            {
                return FpProcessNaN(type1, op1, state);
            }
            else if (type2 == FpType.QnaN)
            {
                return FpProcessNaN(type2, op2, state);
            }

            done = false;

            return FpZero(false);
        }

        private static float FpProcessNaNs3(
            FpType type1,
            FpType type2,
            FpType type3,
            uint op1,
            uint op2,
            uint op3,
            AThreadState state,
            out bool done)
        {
            done = true;

            if (type1 == FpType.SnaN)
            {
                return FpProcessNaN(type1, op1, state);
            }
            else if (type2 == FpType.SnaN)
            {
                return FpProcessNaN(type2, op2, state);
            }
            else if (type3 == FpType.SnaN)
            {
                return FpProcessNaN(type3, op3, state);
            }
            else if (type1 == FpType.QnaN)
            {
                return FpProcessNaN(type1, op1, state);
            }
            else if (type2 == FpType.QnaN)
            {
                return FpProcessNaN(type2, op2, state);
            }
            else if (type3 == FpType.QnaN)
            {
                return FpProcessNaN(type3, op3, state);
            }

            done = false;

            return FpZero(false);
        }

        private static float FpProcessNaN(FpType type, uint op, AThreadState state)
        {
            if (type == FpType.SnaN)
            {
                op |= 1u << 22;

                FpProcessException(FpExc.InvalidOp, state);
            }

            if (state.GetFpcrFlag(Fpcr.Dn))
            {
                return FpDefaultNaN();
            }

            return BitConverter.Int32BitsToSingle((int)op);
        }

        private static void FpProcessException(FpExc exc, AThreadState state)
        {
            int enable = (int)exc + 8;

            if ((state.Fpcr & (1 << enable)) != 0)
            {
                throw new NotImplementedException("floating-point trap handling");
            }
            else
            {
                state.Fpsr |= 1 << (int)exc;
            }
        }
    }

    static class ASoftFloat64
    {
        public static double FpAdd(double value1, double value2, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_64.FPAdd: State.Fpcr = 0x{state.Fpcr:X8}");

            value1 = value1.FpUnpack(out FpType type1, out bool sign1, out ulong op1);
            value2 = value2.FpUnpack(out FpType type2, out bool sign2, out ulong op2);

            double result = FpProcessNaNs(type1, type2, op1, op2, state, out bool done);

            if (!done)
            {
                bool inf1 = type1 == FpType.Infinity; bool zero1 = type1 == FpType.Zero;
                bool inf2 = type2 == FpType.Infinity; bool zero2 = type2 == FpType.Zero;

                if (inf1 && inf2 && sign1 == !sign2)
                {
                    result = FpDefaultNaN();

                    FpProcessException(FpExc.InvalidOp, state);
                }
                else if ((inf1 && !sign1) || (inf2 && !sign2))
                {
                    result = FpInfinity(false);
                }
                else if ((inf1 && sign1) || (inf2 && sign2))
                {
                    result = FpInfinity(true);
                }
                else if (zero1 && zero2 && sign1 == sign2)
                {
                    result = FpZero(sign1);
                }
                else
                {
                    result = value1 + value2;
                }
            }

            return result;
        }

        public static double FpDiv(double value1, double value2, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_64.FPDiv: State.Fpcr = 0x{state.Fpcr:X8}");

            value1 = value1.FpUnpack(out FpType type1, out bool sign1, out ulong op1);
            value2 = value2.FpUnpack(out FpType type2, out bool sign2, out ulong op2);

            double result = FpProcessNaNs(type1, type2, op1, op2, state, out bool done);

            if (!done)
            {
                bool inf1 = type1 == FpType.Infinity; bool zero1 = type1 == FpType.Zero;
                bool inf2 = type2 == FpType.Infinity; bool zero2 = type2 == FpType.Zero;

                if ((inf1 && inf2) || (zero1 && zero2))
                {
                    result = FpDefaultNaN();

                    FpProcessException(FpExc.InvalidOp, state);
                }
                else if (inf1 || zero2)
                {
                    result = FpInfinity(sign1 ^ sign2);

                    if (!inf1) FpProcessException(FpExc.DivideByZero, state);
                }
                else if (zero1 || inf2)
                {
                    result = FpZero(sign1 ^ sign2);
                }
                else
                {
                    result = value1 / value2;
                }
            }

            return result;
        }

        public static double FpMax(double value1, double value2, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_64.FPMax: State.Fpcr = 0x{state.Fpcr:X8}");

            value1 = value1.FpUnpack(out FpType type1, out bool sign1, out ulong op1);
            value2 = value2.FpUnpack(out FpType type2, out bool sign2, out ulong op2);

            double result = FpProcessNaNs(type1, type2, op1, op2, state, out bool done);

            if (!done)
            {
                if (value1 > value2)
                {
                    if (type1 == FpType.Infinity)
                    {
                        result = FpInfinity(sign1);
                    }
                    else if (type1 == FpType.Zero)
                    {
                        result = FpZero(sign1 && sign2);
                    }
                    else
                    {
                        result = value1;
                    }
                }
                else
                {
                    if (type2 == FpType.Infinity)
                    {
                        result = FpInfinity(sign2);
                    }
                    else if (type2 == FpType.Zero)
                    {
                        result = FpZero(sign1 && sign2);
                    }
                    else
                    {
                        result = value2;
                    }
                }
            }

            return result;
        }

        public static double FpMaxNum(double value1, double value2, AThreadState state)
        {
            Debug.WriteIf(state.Fpcr != 0, "ASoftFloat_64.FPMaxNum: ");

            value1.FpUnpack(out FpType type1, out _, out _);
            value2.FpUnpack(out FpType type2, out _, out _);

            if (type1 == FpType.QnaN && type2 != FpType.QnaN)
            {
                value1 = FpInfinity(true);
            }
            else if (type1 != FpType.QnaN && type2 == FpType.QnaN)
            {
                value2 = FpInfinity(true);
            }

            return FpMax(value1, value2, state);
        }

        public static double FpMin(double value1, double value2, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_64.FPMin: State.Fpcr = 0x{state.Fpcr:X8}");

            value1 = value1.FpUnpack(out FpType type1, out bool sign1, out ulong op1);
            value2 = value2.FpUnpack(out FpType type2, out bool sign2, out ulong op2);

            double result = FpProcessNaNs(type1, type2, op1, op2, state, out bool done);

            if (!done)
            {
                if (value1 < value2)
                {
                    if (type1 == FpType.Infinity)
                    {
                        result = FpInfinity(sign1);
                    }
                    else if (type1 == FpType.Zero)
                    {
                        result = FpZero(sign1 || sign2);
                    }
                    else
                    {
                        result = value1;
                    }
                }
                else
                {
                    if (type2 == FpType.Infinity)
                    {
                        result = FpInfinity(sign2);
                    }
                    else if (type2 == FpType.Zero)
                    {
                        result = FpZero(sign1 || sign2);
                    }
                    else
                    {
                        result = value2;
                    }
                }
            }

            return result;
        }

        public static double FpMinNum(double value1, double value2, AThreadState state)
        {
            Debug.WriteIf(state.Fpcr != 0, "ASoftFloat_64.FPMinNum: ");

            value1.FpUnpack(out FpType type1, out _, out _);
            value2.FpUnpack(out FpType type2, out _, out _);

            if (type1 == FpType.QnaN && type2 != FpType.QnaN)
            {
                value1 = FpInfinity(false);
            }
            else if (type1 != FpType.QnaN && type2 == FpType.QnaN)
            {
                value2 = FpInfinity(false);
            }

            return FpMin(value1, value2, state);
        }

        public static double FpMul(double value1, double value2, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_64.FPMul: State.Fpcr = 0x{state.Fpcr:X8}");

            value1 = value1.FpUnpack(out FpType type1, out bool sign1, out ulong op1);
            value2 = value2.FpUnpack(out FpType type2, out bool sign2, out ulong op2);

            double result = FpProcessNaNs(type1, type2, op1, op2, state, out bool done);

            if (!done)
            {
                bool inf1 = type1 == FpType.Infinity; bool zero1 = type1 == FpType.Zero;
                bool inf2 = type2 == FpType.Infinity; bool zero2 = type2 == FpType.Zero;

                if ((inf1 && zero2) || (zero1 && inf2))
                {
                    result = FpDefaultNaN();

                    FpProcessException(FpExc.InvalidOp, state);
                }
                else if (inf1 || inf2)
                {
                    result = FpInfinity(sign1 ^ sign2);
                }
                else if (zero1 || zero2)
                {
                    result = FpZero(sign1 ^ sign2);
                }
                else
                {
                    result = value1 * value2;
                }
            }

            return result;
        }

        public static double FpMulAdd(double valueA, double value1, double value2, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_64.FPMulAdd: State.Fpcr = 0x{state.Fpcr:X8}");

            valueA = valueA.FpUnpack(out FpType typeA, out bool signA, out ulong addend);
            value1 = value1.FpUnpack(out FpType type1, out bool sign1, out ulong op1);
            value2 = value2.FpUnpack(out FpType type2, out bool sign2, out ulong op2);

            bool inf1 = type1 == FpType.Infinity; bool zero1 = type1 == FpType.Zero;
            bool inf2 = type2 == FpType.Infinity; bool zero2 = type2 == FpType.Zero;

            double result = FpProcessNaNs3(typeA, type1, type2, addend, op1, op2, state, out bool done);

            if (typeA == FpType.QnaN && ((inf1 && zero2) || (zero1 && inf2)))
            {
                result = FpDefaultNaN();

                FpProcessException(FpExc.InvalidOp, state);
            }

            if (!done)
            {
                bool infA = typeA == FpType.Infinity; bool zeroA = typeA == FpType.Zero;

                bool signP = sign1 ^  sign2;
                bool infP  = inf1  || inf2;
                bool zeroP = zero1 || zero2;

                if ((inf1 && zero2) || (zero1 && inf2) || (infA && infP && signA != signP))
                {
                    result = FpDefaultNaN();

                    FpProcessException(FpExc.InvalidOp, state);
                }
                else if ((infA && !signA) || (infP && !signP))
                {
                    result = FpInfinity(false);
                }
                else if ((infA && signA) || (infP && signP))
                {
                    result = FpInfinity(true);
                }
                else if (zeroA && zeroP && signA == signP)
                {
                    result = FpZero(signA);
                }
                else
                {
                    // TODO: When available, use: T Math.FusedMultiplyAdd(T, T, T);
                    // https://github.com/dotnet/corefx/issues/31903

                    result = valueA + (value1 * value2);
                }
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double FpMulSub(double valueA, double value1, double value2, AThreadState state)
        {
            Debug.WriteIf(state.Fpcr != 0, "ASoftFloat_64.FPMulSub: ");

            value1 = value1.FpNeg();

            return FpMulAdd(valueA, value1, value2, state);
        }

        public static double FpMulX(double value1, double value2, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_64.FPMulX: State.Fpcr = 0x{state.Fpcr:X8}");

            value1 = value1.FpUnpack(out FpType type1, out bool sign1, out ulong op1);
            value2 = value2.FpUnpack(out FpType type2, out bool sign2, out ulong op2);

            double result = FpProcessNaNs(type1, type2, op1, op2, state, out bool done);

            if (!done)
            {
                bool inf1 = type1 == FpType.Infinity; bool zero1 = type1 == FpType.Zero;
                bool inf2 = type2 == FpType.Infinity; bool zero2 = type2 == FpType.Zero;

                if ((inf1 && zero2) || (zero1 && inf2))
                {
                    result = FpTwo(sign1 ^ sign2);
                }
                else if (inf1 || inf2)
                {
                    result = FpInfinity(sign1 ^ sign2);
                }
                else if (zero1 || zero2)
                {
                    result = FpZero(sign1 ^ sign2);
                }
                else
                {
                    result = value1 * value2;
                }
            }

            return result;
        }

        public static double FpRecipStepFused(double value1, double value2, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_64.FPRecipStepFused: State.Fpcr = 0x{state.Fpcr:X8}");

            value1 = value1.FpNeg();

            value1 = value1.FpUnpack(out FpType type1, out bool sign1, out ulong op1);
            value2 = value2.FpUnpack(out FpType type2, out bool sign2, out ulong op2);

            double result = FpProcessNaNs(type1, type2, op1, op2, state, out bool done);

            if (!done)
            {
                bool inf1 = type1 == FpType.Infinity; bool zero1 = type1 == FpType.Zero;
                bool inf2 = type2 == FpType.Infinity; bool zero2 = type2 == FpType.Zero;

                if ((inf1 && zero2) || (zero1 && inf2))
                {
                    result = FpTwo(false);
                }
                else if (inf1 || inf2)
                {
                    result = FpInfinity(sign1 ^ sign2);
                }
                else
                {
                    // TODO: When available, use: T Math.FusedMultiplyAdd(T, T, T);
                    // https://github.com/dotnet/corefx/issues/31903

                    result = 2d + (value1 * value2);
                }
            }

            return result;
        }

        public static double FpRecpX(double value, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_64.FPRecpX: State.Fpcr = 0x{state.Fpcr:X8}");

            value.FpUnpack(out FpType type, out bool sign, out ulong op);

            double result;

            if (type == FpType.SnaN || type == FpType.QnaN)
            {
                result = FpProcessNaN(type, op, state);
            }
            else
            {
                ulong notExp = (~op >> 52) & 0x7FFul;
                ulong maxExp = 0x7FEul;

                result = BitConverter.Int64BitsToDouble(
                    (long)((sign ? 1ul : 0ul) << 63 | (notExp == 0x7FFul ? maxExp : notExp) << 52));
            }

            return result;
        }

        public static double FprSqrtStepFused(double value1, double value2, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_64.FPRSqrtStepFused: State.Fpcr = 0x{state.Fpcr:X8}");

            value1 = value1.FpNeg();

            value1 = value1.FpUnpack(out FpType type1, out bool sign1, out ulong op1);
            value2 = value2.FpUnpack(out FpType type2, out bool sign2, out ulong op2);

            double result = FpProcessNaNs(type1, type2, op1, op2, state, out bool done);

            if (!done)
            {
                bool inf1 = type1 == FpType.Infinity; bool zero1 = type1 == FpType.Zero;
                bool inf2 = type2 == FpType.Infinity; bool zero2 = type2 == FpType.Zero;

                if ((inf1 && zero2) || (zero1 && inf2))
                {
                    result = FpOnePointFive(false);
                }
                else if (inf1 || inf2)
                {
                    result = FpInfinity(sign1 ^ sign2);
                }
                else
                {
                    // TODO: When available, use: T Math.FusedMultiplyAdd(T, T, T);
                    // https://github.com/dotnet/corefx/issues/31903

                    result = (3d + (value1 * value2)) / 2d;
                }
            }

            return result;
        }

        public static double FpSqrt(double value, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_64.FPSqrt: State.Fpcr = 0x{state.Fpcr:X8}");

            value = value.FpUnpack(out FpType type, out bool sign, out ulong op);

            double result;

            if (type == FpType.SnaN || type == FpType.QnaN)
            {
                result = FpProcessNaN(type, op, state);
            }
            else if (type == FpType.Zero)
            {
                result = FpZero(sign);
            }
            else if (type == FpType.Infinity && !sign)
            {
                result = FpInfinity(sign);
            }
            else if (sign)
            {
                result = FpDefaultNaN();

                FpProcessException(FpExc.InvalidOp, state);
            }
            else
            {
                result = Math.Sqrt(value);
            }

            return result;
        }

        public static double FpSub(double value1, double value2, AThreadState state)
        {
            Debug.WriteLineIf(state.Fpcr != 0, $"ASoftFloat_64.FPSub: State.Fpcr = 0x{state.Fpcr:X8}");

            value1 = value1.FpUnpack(out FpType type1, out bool sign1, out ulong op1);
            value2 = value2.FpUnpack(out FpType type2, out bool sign2, out ulong op2);

            double result = FpProcessNaNs(type1, type2, op1, op2, state, out bool done);

            if (!done)
            {
                bool inf1 = type1 == FpType.Infinity; bool zero1 = type1 == FpType.Zero;
                bool inf2 = type2 == FpType.Infinity; bool zero2 = type2 == FpType.Zero;

                if (inf1 && inf2 && sign1 == sign2)
                {
                    result = FpDefaultNaN();

                    FpProcessException(FpExc.InvalidOp, state);
                }
                else if ((inf1 && !sign1) || (inf2 && sign2))
                {
                    result = FpInfinity(false);
                }
                else if ((inf1 && sign1) || (inf2 && !sign2))
                {
                    result = FpInfinity(true);
                }
                else if (zero1 && zero2 && sign1 == !sign2)
                {
                    result = FpZero(sign1);
                }
                else
                {
                    result = value1 - value2;
                }
            }

            return result;
        }

        private static double FpDefaultNaN()
        {
            return -double.NaN;
        }

        private static double FpInfinity(bool sign)
        {
            return sign ? double.NegativeInfinity : double.PositiveInfinity;
        }

        private static double FpZero(bool sign)
        {
            return sign ? -0d : +0d;
        }

        private static double FpTwo(bool sign)
        {
            return sign ? -2d : +2d;
        }

        private static double FpOnePointFive(bool sign)
        {
            return sign ? -1.5d : +1.5d;
        }

        private static double FpNeg(this double value)
        {
            return -value;
        }

        private static double FpUnpack(this double value, out FpType type, out bool sign, out ulong valueBits)
        {
            valueBits = (ulong)BitConverter.DoubleToInt64Bits(value);

            sign = (~valueBits & 0x8000000000000000ul) == 0ul;

            if ((valueBits & 0x7FF0000000000000ul) == 0ul)
            {
                if ((valueBits & 0x000FFFFFFFFFFFFFul) == 0ul)
                {
                    type = FpType.Zero;
                }
                else
                {
                    type = FpType.Nonzero;
                }
            }
            else if ((~valueBits & 0x7FF0000000000000ul) == 0ul)
            {
                if ((valueBits & 0x000FFFFFFFFFFFFFul) == 0ul)
                {
                    type = FpType.Infinity;
                }
                else
                {
                    type = (~valueBits & 0x0008000000000000ul) == 0ul
                        ? FpType.QnaN
                        : FpType.SnaN;

                    return FpZero(sign);
                }
            }
            else
            {
                type = FpType.Nonzero;
            }

            return value;
        }

        private static double FpProcessNaNs(
            FpType type1,
            FpType type2,
            ulong op1,
            ulong op2,
            AThreadState state,
            out bool done)
        {
            done = true;

            if (type1 == FpType.SnaN)
            {
                return FpProcessNaN(type1, op1, state);
            }
            else if (type2 == FpType.SnaN)
            {
                return FpProcessNaN(type2, op2, state);
            }
            else if (type1 == FpType.QnaN)
            {
                return FpProcessNaN(type1, op1, state);
            }
            else if (type2 == FpType.QnaN)
            {
                return FpProcessNaN(type2, op2, state);
            }

            done = false;

            return FpZero(false);
        }

        private static double FpProcessNaNs3(
            FpType type1,
            FpType type2,
            FpType type3,
            ulong op1,
            ulong op2,
            ulong op3,
            AThreadState state,
            out bool done)
        {
            done = true;

            if (type1 == FpType.SnaN)
            {
                return FpProcessNaN(type1, op1, state);
            }
            else if (type2 == FpType.SnaN)
            {
                return FpProcessNaN(type2, op2, state);
            }
            else if (type3 == FpType.SnaN)
            {
                return FpProcessNaN(type3, op3, state);
            }
            else if (type1 == FpType.QnaN)
            {
                return FpProcessNaN(type1, op1, state);
            }
            else if (type2 == FpType.QnaN)
            {
                return FpProcessNaN(type2, op2, state);
            }
            else if (type3 == FpType.QnaN)
            {
                return FpProcessNaN(type3, op3, state);
            }

            done = false;

            return FpZero(false);
        }

        private static double FpProcessNaN(FpType type, ulong op, AThreadState state)
        {
            if (type == FpType.SnaN)
            {
                op |= 1ul << 51;

                FpProcessException(FpExc.InvalidOp, state);
            }

            if (state.GetFpcrFlag(Fpcr.Dn))
            {
                return FpDefaultNaN();
            }

            return BitConverter.Int64BitsToDouble((long)op);
        }

        private static void FpProcessException(FpExc exc, AThreadState state)
        {
            int enable = (int)exc + 8;

            if ((state.Fpcr & (1 << enable)) != 0)
            {
                throw new NotImplementedException("floating-point trap handling");
            }
            else
            {
                state.Fpsr |= 1 << (int)exc;
            }
        }
    }
}
