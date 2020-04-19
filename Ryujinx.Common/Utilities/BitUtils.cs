using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ryujinx.Common
{
    public static class BitUtils
    {
        [MethodImpl(MethodOptions.FastInline)]
        public static int AlignUp(int value, int size) => unchecked((value + (size - 1)) & -size);

        [MethodImpl(MethodOptions.FastInline)]
        public static ulong AlignUp(ulong value, int size) => unchecked((ulong)AlignUp((long)value, size));

        [MethodImpl(MethodOptions.FastInline)]
        public static long AlignUp(long value, int size) => unchecked((value + (size - 1)) & -(long)size);

        [MethodImpl(MethodOptions.FastInline)]
        public static int AlignDown(int value, int size) => unchecked(value & -size);


        [MethodImpl(MethodOptions.FastInline)]
        public static ulong AlignDown(ulong value, int size) => unchecked((ulong)AlignDown((long)value, size));

        [MethodImpl(MethodOptions.FastInline)]
        public static long AlignDown(long value, int size) => unchecked(value & -(long)size);

        [MethodImpl(MethodOptions.FastInline)]
        public static int DivRoundUp(int value, int dividend) => (value + dividend - 1) / dividend;

        [MethodImpl(MethodOptions.FastInline)]
        public static ulong DivRoundUp(ulong value, uint dividend) => (value + dividend - 1) / dividend;

        [MethodImpl(MethodOptions.FastInline)]
        public static long DivRoundUp(long value, int dividend) => (value + dividend - 1) / dividend;

        [MethodImpl(MethodOptions.FastInline)]
        public static int Pow2RoundUp(int value)
        {
            value--;

            value |= (value >>  1);
            value |= (value >>  2);
            value |= (value >>  4);
            value |= (value >>  8);
            value |= (value >> 16);

            return ++value;
        }

        [MethodImpl(MethodOptions.FastInline)]
        public static int Pow2RoundDown(int value) => IsPowerOfTwo(value) ? value : Pow2RoundUp(value) >> 1;

        [MethodImpl(MethodOptions.FastInline)]
        public static bool IsPowerOfTwo(int value) => (value != 0) && unchecked(value & (value - 1)) == 0;

        [MethodImpl(MethodOptions.FastInline)]
        public static bool IsPowerOfTwo(long value) => (value != 0L) && unchecked(value & (value - 1L)) == 0L;

        [MethodImpl(MethodOptions.FastInline)]
        public static int CountLeadingZeros(int value) => BitOperations.LeadingZeroCount(unchecked((uint)value));

        [MethodImpl(MethodOptions.FastInline)]
        public static int CountLeadingZeros(long value) => BitOperations.LeadingZeroCount(unchecked((ulong)value));

        [MethodImpl(MethodOptions.FastInline)]
        public static int CountTrailingZeros(int value) => BitOperations.TrailingZeroCount(value);

        [MethodImpl(MethodOptions.FastInline)]
        public static long ReverseBits(long value) => unchecked((long)ReverseBits((ulong)value));

        [MethodImpl(MethodOptions.FastInline)]
        private static ulong ReverseBits(ulong value)
        {
            value = ((value & 0xaaaaaaaaaaaaaaaa) >> 1 ) | ((value & 0x5555555555555555) << 1 );
            value = ((value & 0xcccccccccccccccc) >> 2 ) | ((value & 0x3333333333333333) << 2 );
            value = ((value & 0xf0f0f0f0f0f0f0f0) >> 4 ) | ((value & 0x0f0f0f0f0f0f0f0f) << 4 );
            value = ((value & 0xff00ff00ff00ff00) >> 8 ) | ((value & 0x00ff00ff00ff00ff) << 8 );
            value = ((value & 0xffff0000ffff0000) >> 16) | ((value & 0x0000ffff0000ffff) << 16);

            return (value >> 32) | (value << 32);
        }
    }
}
