using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.Memory.Range
{
    public struct PagedMemoryRangeEnumerator
    {
        private readonly ulong _startAddress;
        private readonly int _size;
        private readonly int _pageSize;
        private readonly ulong _pageMask;
        private readonly Func<ulong, ulong> _mapAddress;
        private int _offset;
        private MemoryRange? _current;

        public PagedMemoryRangeEnumerator(ulong startAddress, int size, int pageSize, Func<ulong, ulong> mapAddress)
        {
            _startAddress = startAddress;
            _size = size;
            _pageSize = pageSize;
            _pageMask = (ulong)pageSize - 1;
            _mapAddress = mapAddress;
            _offset = 0;
        }

        public readonly MemoryRange Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current!.Value;
        }

        internal readonly bool HasCurrent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current.HasValue;
        }

        /// <summary>
        /// Returning this through a GetEnumerator() call allows it to be used directly in a foreach loop.
        /// </summary>
        public readonly PagedMemoryRangeEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (_offset == 0 && (_startAddress & _pageMask) != 0)
            {
                ulong rangeAddress = _mapAddress(_startAddress);

                int rangeSize = Math.Min(_size, _pageSize - (int)(_startAddress & _pageMask));

                SetCurrent(rangeAddress, rangeSize);

                return true;
            }

            if (_offset < _size)
            {
                ulong rangeAddress = _mapAddress(_startAddress + (ulong)_offset);

                int rangeSize = Math.Min(_size - _offset, _pageSize);

                SetCurrent(rangeAddress, rangeSize);

                return true;
            }

            _current = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetCurrent(ulong address, int size)
        {
            _current = new MemoryRange(address, (ulong)size);
            _offset += size;
        }
    }
}
