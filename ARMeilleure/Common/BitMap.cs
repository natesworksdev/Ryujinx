using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ARMeilleure.Common
{
    sealed class BitMap : IEnumerator<int>
    {
        private const int IntSize = 64;
        private const int IntMask = IntSize - 1;

        private readonly List<long> _masks;

        private int _enumIndex;
        private long _enumMask;
        private int _enumBit;

        public int Current => _enumIndex * IntSize + _enumBit;

        object IEnumerator.Current => Current;

        [MethodImpl(MethodOptions.FastInline)]
        public BitMap()
        {
            _masks = new List<long>(0);
        }

        [MethodImpl(MethodOptions.FastInline)]
        internal BitMap(int initialCapacity)
        {
            int count = (initialCapacity + IntMask) / IntSize;

            _masks = new List<long>(count);

            while (count-- > 0)
            {
                _masks.Add(0);
            }
        }

        [MethodImpl(MethodOptions.FastInline)]
        internal void Reset(int initialCapacity)
        {
            int count = (initialCapacity + IntMask) / IntSize;

            if (count > _masks.Capacity)
            {
                _masks.Capacity = count;
            }

            _masks.Clear();

            while (count-- > 0)
            {
                _masks.Add(0);
            }
        }

        [MethodImpl(MethodOptions.FastInline)]
        internal bool Set(int bit)
        {
            EnsureCapacity(bit + 1);

            int wordIndex = bit / IntSize;
            int wordBit   = bit & IntMask;

            long wordMask = 1L << wordBit;

            if ((_masks[wordIndex] & wordMask) != 0)
            {
                return false;
            }

            _masks[wordIndex] |= wordMask;

            return true;
        }

        [MethodImpl(MethodOptions.FastInline)]
        internal void Clear(int bit)
        {
            EnsureCapacity(bit + 1);

            int wordIndex = bit / IntSize;
            int wordBit   = bit & IntMask;

            long wordMask = 1L << wordBit;

            _masks[wordIndex] &= ~wordMask;
        }

        [MethodImpl(MethodOptions.FastInline)]
        internal bool IsSet(int bit)
        {
            EnsureCapacity(bit + 1);

            int wordIndex = bit / IntSize;
            int wordBit   = bit & IntMask;

            return (_masks[wordIndex] & (1L << wordBit)) != 0;
        }

        [MethodImpl(MethodOptions.FastInline)]
        internal bool Set(BitMap map)
        {
            EnsureCapacity(map._masks.Count * IntSize);

            bool modified = false;

            for (int index = 0; index < _masks.Count; index++)
            {
                long newValue = _masks[index] | map._masks[index];

                if (_masks[index] != newValue)
                {
                    _masks[index] = newValue;

                    modified = true;
                }
            }

            return modified;
        }

        [MethodImpl(MethodOptions.FastInline)]
        internal bool Clear(BitMap map)
        {
            EnsureCapacity(map._masks.Count * IntSize);

            bool modified = false;

            for (int index = 0; index < _masks.Count; index++)
            {
                long newValue = _masks[index] & ~map._masks[index];

                if (_masks[index] != newValue)
                {
                    _masks[index] = newValue;

                    modified = true;
                }
            }

            return modified;
        }

        [MethodImpl(MethodOptions.FastInline)]
        private void EnsureCapacity(int size)
        {
            while (_masks.Count * IntSize < size)
            {
                _masks.Add(0);
            }
        }

        #region IEnumerable<int> Methods

        // Note: The bit enumerator is embedded in this class to avoid creating garbage when enumerating.

        [MethodImpl(MethodOptions.FastInline)]
        public IEnumerator<int> GetEnumerator()
        {
            Reset();
            return this;
        }

        [MethodImpl(MethodOptions.FastInline)]
        public bool MoveNext()
        {
            if (_enumMask != 0)
            {
                _enumMask &= ~(1L << _enumBit);
            }
            while (_enumMask == 0)
            {
                if (++_enumIndex >= _masks.Count)
                {
                    return false;
                }
                _enumMask = _masks[_enumIndex];
            }
            _enumBit = BitOperations.TrailingZeroCount(_enumMask);
            return true;
        }

        [MethodImpl(MethodOptions.FastInline)]
        public void Reset()
        {
            _enumIndex = -1;
            _enumMask = 0;
            _enumBit = 0;
        }

        [MethodImpl(MethodOptions.FastInline)]
        public void Dispose() { }

        #endregion
    }
}