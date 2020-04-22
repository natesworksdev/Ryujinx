using System;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ARMeilleure.Common
{
    static class BitUtils
    {
        private static readonly sbyte[] HbsNibbleLut = { -1, 0, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3 };

        /// <summary>Returns the highest set bit.</summary>
        /// <param name="value">The value to get the highest set bit from.</param>
        /// <returns>The index of the highest set bit.</returns>
        [MethodImpl(MethodOptions.FastInline)]
        internal static int HighestBitSet(this int value) =>
            31 - BitOperations.LeadingZeroCount((uint)value);

        /// <summary>Returns the highest set bit for a nibble (value range 0 to 15).</summary>
        /// <param name="value">The nibble to get the highest set bit from. Must be within the range 0 to 15 inclusive.</param>
        /// <returns>The index of the highest set bit.</returns>
        [MethodImpl(MethodOptions.FastInline)]
        internal static int HighestBitSetNibble([Range(0, 15)] int value) => HbsNibbleLut[value];

        /// <summary>Returns a 'long' with the given number of bits set to 1, LSB-justified.</summary>
        /// <param name="bits">The number of bits to be 1.</param>
        /// <returns>A value with the requested number of bits set to 1, LSB-justified</returns>
        [MethodImpl(MethodOptions.FastInline)]
        internal static long OneBits([Range(0, 64)] int bits) =>
            (bits != 64) ? ((1L << bits) - 1L) : (-1L);

        /// <summary>
        /// Replicates the provided bits with a given size across a `long`, tightly packed.
        /// For instance, providing 0xF0, with a size of 8, would result in 0xF0F0F0F0F0F0F0F0. 
        /// </summary>
        /// <param name="bits">The bits to replicate</param>
        /// <param name="size">The number of bits to replicate</param>
        /// <returns>The resultant value of the replicated bits.</returns>
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

        /// <summary>
        /// Replicates the provided bits with a given power-of-two size across a `long`, tightly packed.
        /// For instance, providing 0xF0, with a size of 8, would result in 0xF0F0F0F0F0F0F0F0.
        /// The behavior is equivalent to BitUtils.Replicate, but optimized for power-of-two sizes.
        /// </summary>
        /// <param name="bits">The bits to replicate</param>
        /// <param name="size">The number of bits to replicate</param>
        /// <returns>The resultant value of the replicated bits.</returns>
        [MethodImpl(MethodOptions.FastInline)]
        internal static long ReplicatePow2(long bits, [Range(0, 63)] int size)
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

        /// <summary>Returns if the given value is a power-of-two</summary>
        [MethodImpl(MethodOptions.FastInline)]
        internal static bool IsPowerOfTwo(int value) =>
            (value != 0) && (value & (value - 1)) == 0;

        #region Rotations

        [MethodImpl(MethodOptions.FastInline)]
        internal static int RotateRight(this int bits, [Range(0, 31)] int shift, [Range(0, 32)] int size) =>
            (int)RotateRight((uint)bits, shift, size);

        [MethodImpl(MethodOptions.FastInline)]
        internal static uint RotateRight(this uint bits, [Range(0, 31)] int shift, [Range(0, 32)] int size)
        {
            return (size == 32) ?
                BitOperations.RotateRight(bits, shift) :
                (bits >> shift) | (bits << (size - shift));
        }

        [MethodImpl(MethodOptions.FastInline)]
        internal static long RotateRight(this long bits, [Range(0, 63)] int shift, [Range(0, 64)] int size) =>
            (long)RotateRight((ulong)bits, shift, size);

        [MethodImpl(MethodOptions.FastInline)]
        internal static ulong RotateRight(this ulong bits, [Range(0, 63)] int shift, [Range(0, 64)] int size)
        {
            return (size == 64) ?
                BitOperations.RotateRight(bits, shift) :
                (bits >> shift) | (bits << (size - shift));
        }

        #endregion
    }
}
