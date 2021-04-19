using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ARMeilleure.Common
{
    /// <summary>
    /// Represents a table of guest address to a value.
    /// </summary>
    /// <typeparam name="TEntry">Type of the value</typeparam>
    unsafe class AddressTable<TEntry> : IDisposable where TEntry : unmanaged
    {
        public const ulong Mask = ((1ul << 47) - 1) << 2;

        private bool _disposed;
        private TEntry**** _table;
        private readonly TEntry _fill;
        private readonly List<IntPtr> _pages;

        /// <summary>
        /// Gets the base address of the <see cref="EntryTable{TEntry}"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException"><see cref="EntryTable{TEntry}"/> instance was disposed</exception>
        public IntPtr Base
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(null);
                }

                lock (_pages)
                {
                    return (IntPtr)GetRootPage();
                }
            }
        }

        /// <summary>
        /// Constructs a new instance of the <see cref="AddressTable{TEntry}"/> class with the specified default fill
        /// value.
        /// </summary>
        /// <param name="fill">Default fill value</param>
        public AddressTable(TEntry fill)
        {
            _fill = fill;
            _pages = new List<IntPtr>(capacity: 16);
        }

        /// <summary>
        /// Determines if the specified <paramref name="address"/> is mapped on to the table.
        /// </summary>
        /// <param name="address">Guest address</param>
        /// <returns><see langword="true"/> if is mapped; <see langword="false"/> otherwise</returns>
        public bool IsMapped(ulong address)
        {
            return (address & ~Mask) == 0;
        }

        /// <summary>
        /// Gets a reference to the value at the specified guest <paramref name="address"/>.
        /// </summary>
        /// <param name="address">Guest address</param>
        /// <returns>Reference to the value at the specified guest <paramref name="address"/></returns>
        /// <exception cref="ObjectDisposedException"><see cref="EntryTable{TEntry}"/> instance was disposed</exception>
        /// <exception cref="ArgumentException"><paramref name="address"/> is not mapped</exception>
        public ref TEntry GetValue(ulong address)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(null);
            }

            if (!IsMapped(address))
            {
                throw new ArgumentException($"Address 0x{address:X2} is not mapped onto the table.", nameof(address));
            }

            lock (_pages)
            {
                return ref GetPage(address)[(int)(address >> 0x2 & 0x7FFFF)];
            }
        }

        /// <summary>
        /// Gets the leaf page for the specified guest <paramref name="address"/>.
        /// </summary>
        /// <param name="address">Guest address</param>
        /// <returns>Leaf page for the specified guest <paramref name="address"/></returns>
        private TEntry* GetPage(ulong address)
        {
            var level3 = GetRootPage();
            var level2 = (TEntry***)GetNextPage<IntPtr>((void**)level3, (int)(address >> 39 & 0x1FF), 1 << 9);
            var level1 = (TEntry**)GetNextPage<IntPtr>((void**)level2, (int)(address >> 30 & 0x1FF), 1 << 9);
            var level0 = (TEntry*)GetNextPage<TEntry>((void**)level1, (int)(address >> 21 & 0x1FF), 1 << 19, _fill);

            return level0;
        }

        /// <summary>
        /// Lazily initialize and get the root page of the <see cref="AddressTable{TEntry}"/>.
        /// </summary>
        /// <returns>Root page of the <see cref="AddressTable{TEntry}"/></returns>
        private TEntry**** GetRootPage()
        {
            if (_table == null)
            {
                _table = (TEntry****)Allocate(length: 1 << 9, fill: IntPtr.Zero);
            }

            return _table;
        }

        /// <summary>
        /// Gets the next page at the specified index in the specified table. If the next page is
        /// <see langword="null"/>, it is initialized to a page of type <typeparamref name="T"/> of the specified
        /// length.
        /// </summary>
        /// <typeparam name="T">Type of the next page</typeparam>
        /// <param name="level">Current page</param>
        /// <param name="index">Index in the current page</param>
        /// <param name="length">Length of the next page</param>
        /// <param name="fill">Value with which to fill the page if it is initialized</param>
        /// <returns>Next page</returns>
        private void* GetNextPage<T>(void** level, int index, int length, T fill = default) where T : unmanaged
        {
            ref var result = ref level[index];

            if (result == null)
            {
                result = (void*)Allocate(length, fill);
            }

            return result;
        }

        /// <summary>
        /// Allocates a block of memory of the specified type and length.
        /// </summary>
        /// <typeparam name="T">Type of elements</typeparam>
        /// <param name="length">Number of elements</param>
        /// <param name="fill">Fill value</param>
        /// <returns>Allocated block</returns>
        private IntPtr Allocate<T>(int length, T fill) where T : unmanaged
        {
            var page = Marshal.AllocHGlobal(sizeof(T) * length);
            var span = new Span<T>((void*)page, length);

            span.Fill(fill);

            _pages.Add(page);

            return page;
        }

        /// <summary>
        /// Releases all resources used by the <see cref="AddressTable{TEntry}{TEntry}"/> instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all unmanaged and optionally managed resources used by the <see cref="AddressTable{TEntry}"/>
        /// instance.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to dispose managed resources also; otherwise just unmanaged resouces</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                foreach (var page in _pages)
                {
                    Marshal.FreeHGlobal(page);
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Frees resources used by the <see cref="AddressTable{TEntry}"/> instance.
        /// </summary>
        ~AddressTable()
        {
            Dispose(false);
        }
    }
}
