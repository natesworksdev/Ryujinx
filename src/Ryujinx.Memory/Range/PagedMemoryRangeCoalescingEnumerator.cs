using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.Memory.Range
{
    public struct PagedMemoryRangeCoalescingEnumerator
    {
        private PagedMemoryRangeEnumerator _enumerator;
        private MemoryRange? _current;

        public PagedMemoryRangeCoalescingEnumerator(ulong startAddress, int size, int pageSize, Func<ulong, ulong> mapAddress)
        {
            _enumerator = new PagedMemoryRangeEnumerator(startAddress, size, pageSize, mapAddress);
        }

        public readonly MemoryRange Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current!.Value;
        }

        /// <summary>
        /// Returning this through a GetEnumerator() call allows it to be used directly in a foreach loop.
        /// </summary>
        public readonly PagedMemoryRangeCoalescingEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (_current is null)
            {
                if (_enumerator.MoveNext() == false)
                {
                    _current = null;
                    return false;
                }
            }

            if (_enumerator.HasCurrent)
            {
                MemoryRange combinedRange = _enumerator.Current;

                while (_enumerator.MoveNext())
                {
                    MemoryRange nextRange = _enumerator.Current;

                    if (combinedRange.EndAddress == nextRange.Address)
                    {
                        combinedRange = new MemoryRange(combinedRange.Address, combinedRange.Size + nextRange.Size);
                    }
                    else
                    {
                        break;
                    }
                }

                _current = combinedRange;
                return true;
            }

            _current = null;
            return false;
        }
    }
}
