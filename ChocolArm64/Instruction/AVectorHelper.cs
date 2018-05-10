using ChocolArm64.State;
using ChocolArm64.Translation;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ChocolArm64.Instruction
{
    static class AVectorHelper
    {
        public static void EmitCall(AILEmitterCtx Context, string Name64, string Name128)
        {
            bool IsSimd64 = Context.CurrOp.RegisterSize == ARegisterSize.SIMD64;

            Context.EmitCall(typeof(AVectorHelper), IsSimd64 ? Name64 : Name128);
        }

        public static void EmitCall(AILEmitterCtx Context, string MthdName)
        {
            Context.EmitCall(typeof(AVectorHelper), MthdName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SatF32ToS32(float Value)
        {
            if (float.IsNaN(Value)) return 0;

            return Value > int.MaxValue ? int.MaxValue :
                   Value < int.MinValue ? int.MinValue : (int)Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SatF32ToS64(float Value)
        {
            if (float.IsNaN(Value)) return 0;

            return Value > long.MaxValue ? long.MaxValue :
                   Value < long.MinValue ? long.MinValue : (long)Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SatF32ToU32(float Value)
        {
            if (float.IsNaN(Value)) return 0;

            return Value > uint.MaxValue ? uint.MaxValue :
                   Value < uint.MinValue ? uint.MinValue : (uint)Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SatF32ToU64(float Value)
        {
            if (float.IsNaN(Value)) return 0;

            return Value > ulong.MaxValue ? ulong.MaxValue :
                   Value < ulong.MinValue ? ulong.MinValue : (ulong)Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SatF64ToS32(double Value)
        {
            if (double.IsNaN(Value)) return 0;

            return Value > int.MaxValue ? int.MaxValue :
                   Value < int.MinValue ? int.MinValue : (int)Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SatF64ToS64(double Value)
        {
            if (double.IsNaN(Value)) return 0;

            return Value > long.MaxValue ? long.MaxValue :
                   Value < long.MinValue ? long.MinValue : (long)Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SatF64ToU32(double Value)
        {
            if (double.IsNaN(Value)) return 0;

            return Value > uint.MaxValue ? uint.MaxValue :
                   Value < uint.MinValue ? uint.MinValue : (uint)Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SatF64ToU64(double Value)
        {
            if (double.IsNaN(Value)) return 0;

            return Value > ulong.MaxValue ? ulong.MaxValue :
                   Value < ulong.MinValue ? ulong.MinValue : (ulong)Value;
        }

        public static int CountSetBits8(byte Value)
        {
            return ((Value >> 0) & 1) + ((Value >> 1) & 1) +
                   ((Value >> 2) & 1) + ((Value >> 3) & 1) +
                   ((Value >> 4) & 1) + ((Value >> 5) & 1) +
                   ((Value >> 6) & 1) +  (Value >> 7);
        }

        public static float MaxF(float LHS, float RHS)
        {
            if (LHS == 0.0 && RHS == 0.0)
            {
                if (BitConverter.SingleToInt32Bits(LHS) < 0 &&
                    BitConverter.SingleToInt32Bits(RHS) < 0)
                    return -0.0f;

                return 0.0f;
            }

            if (LHS > RHS)
                return LHS;

            if (float.IsNaN(LHS))
                return LHS;

            return RHS;
        }

        public static double Max(double LHS, double RHS)
        {
            if (LHS == 0.0 && RHS == 0.0)
            {
                if (BitConverter.DoubleToInt64Bits(LHS) < 0 &&
                    BitConverter.DoubleToInt64Bits(RHS) < 0)
                    return -0.0;

                return 0.0;
            }

            if (LHS > RHS)
                return LHS;

            if (double.IsNaN(LHS))
                return LHS;

            return RHS;
        }

        public static float MinF(float LHS, float RHS)
        {
            if (LHS == 0.0 && RHS == 0.0)
            {
                if (BitConverter.SingleToInt32Bits(LHS) < 0 ||
                    BitConverter.SingleToInt32Bits(RHS) < 0)
                    return -0.0f;

                return 0.0f;
            }

            if (LHS < RHS)
                return LHS;

            if (float.IsNaN(LHS))
                return LHS;

            return RHS;
        }

        public static double Min(double LHS, double RHS)
        {
            if (LHS == 0.0 && RHS == 0.0)
            {
                if (BitConverter.DoubleToInt64Bits(LHS) < 0 ||
                    BitConverter.DoubleToInt64Bits(RHS) < 0)
                    return -0.0;

                return 0.0;
            }

            if (LHS < RHS)
                return LHS;

            if (double.IsNaN(LHS))
                return LHS;

            return RHS;
        }

        public static float RoundF(float Value, int Fpcr)
        {
            switch ((ARoundMode)((Fpcr >> 22) & 3))
            {
                case ARoundMode.ToNearest:            return MathF.Round   (Value);
                case ARoundMode.TowardsPlusInfinity:  return MathF.Ceiling (Value);
                case ARoundMode.TowardsMinusInfinity: return MathF.Floor   (Value);
                case ARoundMode.TowardsZero:          return MathF.Truncate(Value);
            }

            throw new InvalidOperationException();
        }

        public static double Round(double Value, int Fpcr)
        {
            switch ((ARoundMode)((Fpcr >> 22) & 3))
            {
                case ARoundMode.ToNearest:            return Math.Round   (Value);
                case ARoundMode.TowardsPlusInfinity:  return Math.Ceiling (Value);
                case ARoundMode.TowardsMinusInfinity: return Math.Floor   (Value);
                case ARoundMode.TowardsZero:          return Math.Truncate(Value);
            }

            throw new InvalidOperationException();
        }

        public static Vector128<float> Tbl1_V64(
            Vector128<float> Vector,
            Vector128<float> Tb0)
        {
            return Tbl(Vector, 8, Tb0);
        }

        public static Vector128<float> Tbl1_V128(
            Vector128<float> Vector,
            Vector128<float> Tb0)
        {
            return Tbl(Vector, 16, Tb0);
        }

        public static Vector128<float> Tbl2_V64(
            Vector128<float> Vector,
            Vector128<float> Tb0,
            Vector128<float> Tb1)
        {
            return Tbl(Vector, 8, Tb0, Tb1);
        }

        public static Vector128<float> Tbl2_V128(
            Vector128<float> Vector,
            Vector128<float> Tb0,
            Vector128<float> Tb1)
        {
            return Tbl(Vector, 16, Tb0, Tb1);
        }

        public static Vector128<float> Tbl3_V64(
            Vector128<float> Vector,
            Vector128<float> Tb0,
            Vector128<float> Tb1,
            Vector128<float> Tb2)
        {
            return Tbl(Vector, 8, Tb0, Tb1, Tb2);
        }

        public static Vector128<float> Tbl3_V128(
            Vector128<float> Vector,
            Vector128<float> Tb0,
            Vector128<float> Tb1,
            Vector128<float> Tb2)
        {
            return Tbl(Vector, 16, Tb0, Tb1, Tb2);
        }

        public static Vector128<float> Tbl4_V64(
            Vector128<float> Vector,
            Vector128<float> Tb0,
            Vector128<float> Tb1,
            Vector128<float> Tb2,
            Vector128<float> Tb3)
        {
            return Tbl(Vector, 8, Tb0, Tb1, Tb2, Tb3);
        }

        public static Vector128<float> Tbl4_V128(
            Vector128<float> Vector,
            Vector128<float> Tb0,
            Vector128<float> Tb1,
            Vector128<float> Tb2,
            Vector128<float> Tb3)
        {
            return Tbl(Vector, 16, Tb0, Tb1, Tb2, Tb3);
        }

        private static Vector128<float> Tbl(Vector128<float> Vector, int Bytes, params Vector128<float>[] Tb)
        {
            Vector128<float> Res = new Vector128<float>();

            byte[] Table = new byte[Tb.Length * 16];

            for (byte Index  = 0; Index  < Tb.Length; Index++)
            for (byte Index2 = 0; Index2 < 16;        Index2++)
            {
                Table[Index * 16 + Index2] = (byte)VectorExtractIntZx(Tb[Index], Index2, 0);
            }

            for (byte Index = 0; Index < Bytes; Index++)
            {
                byte TblIdx = (byte)VectorExtractIntZx(Vector, Index, 0);

                if (TblIdx < Table.Length)
                {
                    Res = VectorInsertInt(Table[TblIdx], Res, Index, 0);
                }
            }

            return Res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong VectorExtractIntZx(Vector128<float> Vector, byte Index, int Size)
        {
            if (Sse41.IsSupported)
            {
                switch (Size)
                {
                    case 0:
                        return Sse41.Extract(Sse.StaticCast<float, byte>(Vector), Index);

                    case 1:
                        return Sse2.Extract(Sse.StaticCast<float, ushort>(Vector), Index);

                    case 2:
                        return Sse41.Extract(Sse.StaticCast<float, uint>(Vector), Index);

                    case 3:
                        return Sse41.Extract(Sse.StaticCast<float, ulong>(Vector), Index);
                }

                throw new ArgumentOutOfRangeException(nameof(Size));
            }

            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long VectorExtractIntSx(Vector128<float> Vector, byte Index, int Size)
        {
            if (Sse41.IsSupported)
            {
                switch (Size)
                {
                    case 0:
                        return Sse41.Extract(Sse.StaticCast<float, sbyte>(Vector), Index);

                    case 1:
                        return Sse2.Extract(Sse.StaticCast<float, short>(Vector), Index);

                    case 2:
                        return Sse41.Extract(Sse.StaticCast<float, int>(Vector), Index);

                    case 3:
                        return Sse41.Extract(Sse.StaticCast<float, long>(Vector), Index);
                }

                throw new ArgumentOutOfRangeException(nameof(Size));
            }

            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float VectorExtractSingle(Vector128<float> Vector, byte Index)
        {
            if (Sse41.IsSupported)
            {
                return Sse41.Extract(Vector, Index);
            }

            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double VectorExtractDouble(Vector128<float> Vector, byte Index)
        {
            if (Sse41.IsSupported)
            {
                int FIdx = Index << 1;

                int Low  = BitConverter.SingleToInt32Bits(Sse41.Extract(Vector, (byte)(FIdx + 0)));
                int High = BitConverter.SingleToInt32Bits(Sse41.Extract(Vector, (byte)(FIdx + 1)));

                return BitConverter.Int64BitsToDouble(
                    ((long)(uint)Low  << 0) |
                    ((long)(uint)High << 32));
            }

            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> VectorInsertSingle(float Value, Vector128<float> Vector, byte Index)
        {
            if (Sse41.IsSupported)
            {
                return Sse41.Insert(Vector, Value, (byte)(Index << 4));
            }

            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> VectorInsertDouble(double Value, Vector128<float> Vector, byte Index)
        {
            if (Sse41.IsSupported)
            {
                int FIdx = Index << 5;

                long Raw = BitConverter.DoubleToInt64Bits(Value);

                float Low  = BitConverter.Int32BitsToSingle((int)((ulong)Raw >> 0));
                float High = BitConverter.Int32BitsToSingle((int)((ulong)Raw >> 32));

                Vector = Sse41.Insert(Vector, Low,  (byte)(FIdx + 0));
                Vector = Sse41.Insert(Vector, High, (byte)(FIdx + 0x10));

                return Vector;
            }

            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> VectorInsertInt(ulong Value, Vector128<float> Vector, byte Index, int Size)
        {
            if (Sse41.IsSupported)
            {
                switch (Size)
                {
                    case 0:
                        return Sse.StaticCast<byte, float>(Sse41.Insert(Sse.StaticCast<float, byte>(Vector), (byte)Value, Index));

                    case 1:
                        return Sse.StaticCast<ushort, float>(Sse2.Insert(Sse.StaticCast<float, ushort>(Vector), (ushort)Value, Index));

                    case 2:
                        return Sse.StaticCast<uint, float>(Sse41.Insert(Sse.StaticCast<float, uint>(Vector), (uint)Value, Index));

                    case 3:
                        return Sse.StaticCast<ulong, float>(Sse41.Insert(Sse.StaticCast<float, ulong>(Vector), Value, Index));
                }

                throw new ArgumentOutOfRangeException(nameof(Size));
            }

            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<sbyte> VectorSingleToSByte(Vector128<float> Vector)
        {
            if (Sse.IsSupported)
            {
                return Sse.StaticCast<float, sbyte>(Vector);
            }

            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<short> VectorSingleToInt16(Vector128<float> Vector)
        {
            if (Sse.IsSupported)
            {
                return Sse.StaticCast<float, short>(Vector);
            }

            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<int> VectorSingleToInt32(Vector128<float> Vector)
        {
            if (Sse.IsSupported)
            {
                return Sse.StaticCast<float, int>(Vector);
            }

            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<long> VectorSingleToInt64(Vector128<float> Vector)
        {
            if (Sse.IsSupported)
            {
                return Sse.StaticCast<float, long>(Vector);
            }

            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<double> VectorSingleToDouble(Vector128<float> Vector)
        {
            if (Sse.IsSupported)
            {
                return Sse.StaticCast<float, double>(Vector);
            }

            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> VectorSByteToSingle(Vector128<sbyte> Vector)
        {
            if (Sse.IsSupported)
            {
                return Sse.StaticCast<sbyte, float>(Vector);
            }

            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> VectorInt16ToSingle(Vector128<short> Vector)
        {
            if (Sse.IsSupported)
            {
                return Sse.StaticCast<short, float>(Vector);
            }

            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> VectorInt32ToSingle(Vector128<int> Vector)
        {
            if (Sse.IsSupported)
            {
                return Sse.StaticCast<int, float>(Vector);
            }

            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> VectorInt64ToSingle(Vector128<long> Vector)
        {
            if (Sse.IsSupported)
            {
                return Sse.StaticCast<long, float>(Vector);
            }

            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> VectorDoubleToSingle(Vector128<double> Vector)
        {
            if (Sse.IsSupported)
            {
                return Sse.StaticCast<double, float>(Vector);
            }

            throw new PlatformNotSupportedException();
        }
    }
}