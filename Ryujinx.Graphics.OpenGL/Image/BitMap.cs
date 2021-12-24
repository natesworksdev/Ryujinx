using System.Collections.Generic;
using System.Numerics;

namespace Ryujinx.Graphics.OpenGL.Image
{
    /// <summary>
    /// Represents a list of bits.
    /// </summary>
    class BitMap
    {
        private const int IntSize = 64;
        private const int IntMask = IntSize - 1;

        private readonly List<ulong> _masks;

        /// <summary>
        /// Creates a new instance of the bitmap.
        /// </summary>
        public BitMap()
        {
            _masks = new List<ulong>(0);
        }

        /// <summary>
        /// Creates a new instance of the bitmap.
        /// </summary>
        /// <param name="initialCapacity">Initial size (in bits) that the bitmap can hold</param>
        public BitMap(int initialCapacity)
        {
            int count = (initialCapacity + IntMask) / IntSize;

            _masks = new List<ulong>(count);

            while (count-- > 0)
            {
                _masks.Add(0);
            }
        }

        /// <summary>
        /// Sets a bit on the list to 1.
        /// </summary>
        /// <param name="bit">Index of the bit</param>
        /// <returns>True if the bit value was modified by this operation, false otherwise</returns>
        public bool Set(int bit)
        {
            EnsureCapacity(bit + 1);

            int wordIndex = bit / IntSize;
            int wordBit   = bit & IntMask;

            ulong wordMask = 1UL << wordBit;

            if ((_masks[wordIndex] & wordMask) != 0)
            {
                return false;
            }

            _masks[wordIndex] |= wordMask;

            return true;
        }

        /// <summary>
        /// Sets a bit on the list to 0.
        /// </summary>
        /// <param name="bit">Index of the bit</param>
        public void Clear(int bit)
        {
            EnsureCapacity(bit + 1);

            int wordIndex = bit / IntSize;
            int wordBit   = bit & IntMask;

            ulong wordMask = 1UL << wordBit;

            _masks[wordIndex] &= ~wordMask;
        }

        /// <summary>
        /// Finds the first bit on the list with a value of 0.
        /// </summary>
        /// <returns>Index of the bit with value 0</returns>
        public int FindFirstUnset()
        {
            int index = 0;

            while (index < _masks.Count && _masks[index] == ulong.MaxValue)
            {
                index++;
            }

            if (index == _masks.Count)
            {
                _masks.Add(0);
            }

            int bit = index * IntSize;

            bit += BitOperations.TrailingZeroCount(~_masks[index]);

            return bit;
        }

        /// <summary>
        /// Ensures that the array can hold a given number of bits, resizing as needed.
        /// </summary>
        /// <param name="size">Number of bits</param>
        private void EnsureCapacity(int size)
        {
            while (_masks.Count * IntSize < size)
            {
                _masks.Add(0);
            }
        }

        private int _iterIndex;
        private ulong _iterMask;

        /// <summary>
        /// Starts iterating from bit 0.
        /// </summary>
        public void BeginIterating()
        {
            _iterIndex = 0;
            _iterMask = _masks.Count != 0 ? _masks[0] : 0;
        }

        /// <summary>
        /// Gets the next bit set to 1 on the list.
        /// </summary>
        /// <returns>Index of the bit, or -1 if none found</returns>
        public int GetNext()
        {
            if (_iterIndex >= _masks.Count)
            {
                return -1;
            }

            while (_iterMask == 0 && _iterIndex + 1 < _masks.Count)
            {
                _iterMask = _masks[++_iterIndex];
            }

            if (_iterMask == 0)
            {
                return -1;
            }

            int bit = BitOperations.TrailingZeroCount(_iterMask);

            _iterMask &= ~(1UL << bit);

            return _iterIndex * IntSize + bit;
        }

        /// <summary>
        /// Gets the next bit set to 1 on the list, while also setting it to 0.
        /// </summary>
        /// <returns>Index of the bit, or -1 if none found</returns>
        public int GetNextAndClear()
        {
            if (_iterIndex >= _masks.Count)
            {
                return -1;
            }

            ulong mask = _masks[_iterIndex];

            while (mask == 0 && _iterIndex + 1 < _masks.Count)
            {
                mask = _masks[++_iterIndex];
            }

            if (mask == 0)
            {
                return -1;
            }

            int bit = BitOperations.TrailingZeroCount(mask);

            mask &= ~(1UL << bit);

            _masks[_iterIndex] = mask;

            return _iterIndex * IntSize + bit;
        }
    }
}
