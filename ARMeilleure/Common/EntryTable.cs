using System;

namespace ARMeilleure.Common
{
    /// <summary>
    /// Represents a fixed size table of the type <typeparamref name="TEntry"/>, whose entries will remain at the same
    /// address through out the table's lifetime.
    /// </summary>
    /// <typeparam name="TEntry">Type of the entry in the table</typeparam>
    class EntryTable<TEntry> where TEntry : unmanaged
    {
        private int _freeHint;
        private readonly TEntry[] _table;
        private readonly BitMap _allocated;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntryTable{TValue}"/> class with the specified capacity.
        /// </summary>
        /// <param name="capacity">Capacity of the table</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 0</exception>
        public EntryTable(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _freeHint = 0;
            _allocated = new BitMap();
            _table = GC.AllocateArray<TEntry>(capacity, pinned: true);
        }

        /// <summary>
        /// Tries to allocate an entry in the <see cref="EntryTable{TValue}"/>. Returns <see langword="true"/> if
        /// success; otherwise returns <see langword="false"/>.
        /// </summary>
        /// <param name="index">Index of entry allocated in the table</param>
        /// <returns><see langword="true"/> if success; otherwise <see langword="false"/></returns>
        public bool TryAllocate(out int index)
        {
            lock (_allocated)
            {
                if (_allocated.IsSet(_freeHint))
                {
                    _freeHint = _allocated.FindFirstUnset();
                }

                if (_freeHint < _table.Length)
                {
                    index = checked(_freeHint++);

                    _allocated.Set(index);

                    return true;
                }
            }

            index = 0;

            return false;
        }

        /// <summary>
        /// Frees the entry at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">Index of entry to free</param>
        public void Free(int index)
        {
            lock (_allocated)
            {
                _allocated.Clear(index);
            }
        }

        /// <summary>
        /// Gets a reference to the entry at the specified allocated <paramref name="index"/>.
        /// </summary>
        /// <param name="index">Index of the entry</param>
        /// <returns>Reference to the entry at the specified index</returns>
        /// <exception cref="ArgumentException">Entry at <paramref name="index"/> is not allocated</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside of the table</exception>
        public ref TEntry GetValue(int index)
        {
            if (index < 0 || index >= _table.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            lock (_allocated)
            {
                if (!_allocated.IsSet(index))
                {
                    throw new ArgumentException("Entry at the specified index was not allocated", nameof(index));
                }
            }

            return ref _table[index];
        }
    }
}
