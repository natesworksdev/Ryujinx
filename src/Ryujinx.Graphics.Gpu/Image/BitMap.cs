using System.Numerics;

namespace Ryujinx.Graphics.Gpu.Image
{
    /// <summary>
    /// Represents a list of bits.
    /// </summary>
    class BitMap
    {
        private const int IntSize = 64;

        private const int IntShift = 6;
        private const int IntMask = IntSize - 1;

        private readonly ulong[] _masks;

        /// <summary>
        /// Creates a new instance of the bitmap.
        /// </summary>
        /// <param name="count">Size (in bits) that the bitmap can hold</param>
        public BitMap(int count)
        {
            _masks = new ulong[(count + IntMask) / IntSize];
        }

        /// <summary>
        /// Sets a bit to 1.
        /// </summary>
        /// <param name="bit">Index of the bit</param>
        /// <returns>True if the bit value was modified by this operation, false otherwise</returns>
        public bool Set(int bit)
        {
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
        /// Sets a range of bits to 1.
        /// </summary>
        /// <param name="start">Inclusive index of the first bit to set</param>
        /// <param name="end">Inclusive index of the last bit to set</param>
        public void SetRange(int start, int end)
        {
            if (start == end)
            {
                Set(start);
                return;
            }

            int startIndex = start >> IntShift;
            int startBit = start & IntMask;
            ulong startMask = ulong.MaxValue << startBit;

            int endIndex = end >> IntShift;
            int endBit = end & IntMask;
            ulong endMask = ulong.MaxValue >> (IntMask - endBit);

            if (startIndex == endIndex)
            {
                _masks[startIndex] |= startMask & endMask;
            }
            else
            {
                _masks[startIndex] |= startMask;

                for (int i = startIndex + 1; i < endIndex; i++)
                {
                    _masks[i] = ulong.MaxValue;
                }

                _masks[endIndex] |= endMask;
            }
        }

        /// <summary>
        /// Sets a bit to 0.
        /// </summary>
        /// <param name="bit">Index of the bit</param>
        public void Clear(int bit)
        {
            int wordIndex = bit / IntSize;
            int wordBit   = bit & IntMask;

            ulong wordMask = 1UL << wordBit;

            _masks[wordIndex] &= ~wordMask;
        }

        /// <summary>
        /// Finds the first bit with a value of 0.
        /// </summary>
        /// <returns>Index of the bit with value 0, or -1 if none found</returns>
        public int FindFirstUnset()
        {
            int index = 0;

            while (index < _masks.Length && _masks[index] == ulong.MaxValue)
            {
                index++;
            }

            if (index == _masks.Length)
            {
                return -1;
            }

            int bit = index * IntSize;

            bit += BitOperations.TrailingZeroCount(~_masks[index]);

            return bit;
        }

        private int _iterIndex;
        private ulong _iterMask;

        /// <summary>
        /// Starts iterating from bit 0.
        /// </summary>
        public void BeginIterating()
        {
            _iterIndex = 0;
            _iterMask = _masks.Length != 0 ? _masks[0] : 0;
        }

        /// <summary>
        /// Gets the next bit set to 1.
        /// </summary>
        /// <returns>Index of the bit, or -1 if none found</returns>
        public int GetNext()
        {
            if (_iterIndex >= _masks.Length)
            {
                return -1;
            }

            while (_iterMask == 0 && _iterIndex + 1 < _masks.Length)
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
        /// Gets the next bit set to 1, while also setting it to 0.
        /// </summary>
        /// <returns>Index of the bit, or -1 if none found</returns>
        public int GetNextAndClear()
        {
            if (_iterIndex >= _masks.Length)
            {
                return -1;
            }

            ulong mask = _masks[_iterIndex];

            while (mask == 0 && _iterIndex + 1 < _masks.Length)
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
