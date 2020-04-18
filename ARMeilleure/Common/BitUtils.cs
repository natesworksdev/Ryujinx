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
        internal static int HighestBitSet(this int value) => 31 - BitOperations.LeadingZeroCount((uint)value);

        [MethodImpl(MethodOptions.FastInline)]
        internal static int HighestBitSetNibble([Range(0, 15)] int value) => HbsNibbleLut[value];

        [MethodImpl(MethodOptions.FastInline)]
        internal static long OneBits([Range(0, 64)] int bits) => (bits != 64) ? ((1L << bits) - 1L) : (-1L);

        [MethodImpl(MethodOptions.FastInline)]
        internal static long Replicate(long bits, [Range(0, 63)] int size)
        {
            long output = 0;

            for (int bit = 0; bit < 64; bit += size)
            {
                output |= bits << bit;
            }

            return output;
        }

        #region Rotations

        [MethodImpl(MethodOptions.FastInline)]
        internal static int RotateRight(this int bits, [Range(0, 32)] int shift, [Range(0, 32)] int size = 32) => (int)RotateRight((uint)bits, shift, size);

        [MethodImpl(MethodOptions.FastInline)]
        internal static uint RotateRight(this uint bits, [Range(0, 32)] int shift, [Range(0, 32)] int size = 32)
        {
            return size == 32 ?
                BitOperations.RotateRight(bits, shift) :
                (bits >> shift) | (bits << (size - shift));
        }

        [MethodImpl(MethodOptions.FastInline)]
        internal static long RotateRight(this long bits, [Range(0, 64)] int shift, [Range(0, 64)] int size = 64) => (long)RotateRight((ulong)bits, shift, size);

        [MethodImpl(MethodOptions.FastInline)]
        internal static ulong RotateRight(this ulong bits, [Range(0, 64)] int shift, [Range(0, 64)] int size = 64)
        {
            return size == 64 ?
                BitOperations.RotateRight(bits, shift) :
                (bits >> shift) | (bits << (size - shift));
        }

        #endregion
    }
}
