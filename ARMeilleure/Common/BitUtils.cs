using System;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ARMeilleure.Common
{
    internal static class BitUtils
    {
        private static readonly sbyte[] HbsNibbleLut = { -1, 0, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3 };

        [MethodImpl(MethodOptions.FastInline)]
        internal static int CountBits(this int value) => BitOperations.PopCount(unchecked((uint)value));

        [MethodImpl(MethodOptions.FastInline)]
        internal static int HighestBitSet(this int value) => 31 - BitOperations.LeadingZeroCount(unchecked((uint)value));

        [MethodImpl(MethodOptions.FastInline)]
        internal static int HighestBitSetNibble([Range(0, 15)] int value) => HbsNibbleLut[value];

        [MethodImpl(MethodOptions.FastInline)]
        internal static long OneBits([Range(0, 64)] int bits) => unchecked((bits != 64) ? ((1L << bits) - 1L) : (-1L));

        [MethodImpl(MethodOptions.FastInline)]
        internal static long Replicate(long bits, [Range(0, 63)] int size)
        {
            // Size is always a power of two, so we can just duplicate
            // output over itself as we build it.
#if DEBUG
            if (!IsPowerOfTwo(size))
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }
#endif

            long output = bits;

            for (int bit = size; bit < 64; bit <<= 1)
            {
                output |= output << bit;
            }

            return output;
        }

        [MethodImpl(MethodOptions.FastInline)]
        internal static bool IsPowerOfTwo(int value) => (value != 0) && (value & (value - 1)) == 0;

        #region Rotations

        [MethodImpl(MethodOptions.FastInline)]
        internal static int RotateRight(this int bits, [Range(0, 31)] int shift, [Range(0, 32)] int size) => unchecked((int)RotateRight((uint)bits, shift, size));

        [MethodImpl(MethodOptions.FastInline)]
        internal static uint RotateRight(this uint bits, [Range(0, 31)] int shift, [Range(0, 32)] int size)
        {
            return (size == 32) ?
                BitOperations.RotateRight(bits, shift) :
                (bits >> shift) | (bits << (size - shift));
        }

        [MethodImpl(MethodOptions.FastInline)]
        internal static int RotateRight(this int bits, [Range(0, 31)] int shift) => unchecked((int)RotateRight((uint)bits, shift));

        [MethodImpl(MethodOptions.FastInline)]
        internal static uint RotateRight(this uint bits, [Range(0, 31)] int shift) => BitOperations.RotateRight(bits, shift);

        [MethodImpl(MethodOptions.FastInline)]
        internal static long RotateRight(this long bits, [Range(0, 63)] int shift, [Range(0, 64)] int size) => unchecked((long)RotateRight((ulong)bits, shift, size));

        [MethodImpl(MethodOptions.FastInline)]
        internal static ulong RotateRight(this ulong bits, [Range(0, 63)] int shift, [Range(0, 64)] int size)
        {
            return (size == 64) ?
                BitOperations.RotateRight(bits, shift) :
                (bits >> shift) | (bits << (size - shift));
        }

        [MethodImpl(MethodOptions.FastInline)]
        internal static long RotateRight(this long bits, [Range(0, 63)] int shift) => unchecked((long)RotateRight((ulong)bits, shift));

        [MethodImpl(MethodOptions.FastInline)]
        internal static ulong RotateRight(this ulong bits, [Range(0, 63)] int shift) => BitOperations.RotateRight(bits, shift);

        #endregion
    }
}
