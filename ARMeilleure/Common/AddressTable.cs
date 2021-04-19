using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ARMeilleure.Common
{
    unsafe class AddressTable<TEntry> : IDisposable where TEntry : unmanaged
    {
        private bool _disposed;
        private TEntry**** _table;
        private readonly TEntry _fill;
        private readonly List<IntPtr> _pages;

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

        public AddressTable(TEntry fill)
        {
            _fill = fill;
            _pages = new List<IntPtr>(capacity: 16);
        }

        public ref TEntry GetValue(ulong address)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(null);
            }

            lock (_pages)
            {
                return ref GetPage(address)[(int)(address >> 0x2 & 0x7FFFF)];
            }
        }

        private TEntry* GetPage(ulong address)
        {
            var level3 = GetRootPage();
            var level2 = (TEntry***)GetNextPage<IntPtr>((void**)level3, (int)(address >> 39 & 0x1FF), 1 << 9);
            var level1 = (TEntry**)GetNextPage<IntPtr>((void**)level2, (int)(address >> 30 & 0x1FF), 1 << 9);
            var level0 = (TEntry*)GetNextPage<TEntry>((void**)level1, (int)(address >> 21 & 0x1FF), 1 << 19, _fill);

            return level0;
        }

        private TEntry**** GetRootPage()
        {
            if (_table == null)
            {
                _table = (TEntry****)Allocate(length: 1 << 9, fill: IntPtr.Zero);
            }

            return _table;
        }

        private void* GetNextPage<T>(void** level, int index, int length, T fill = default) where T : unmanaged
        {
            ref var result = ref level[index];

            if (result == null)
            {
                result = (void*)Allocate(length, fill);
            }

            return result;
        }

        private IntPtr Allocate<T>(int length, T fill) where T : unmanaged
        {
            var page = Marshal.AllocHGlobal(sizeof(T) * length);
            var span = new Span<T>((void*)page, length);

            span.Fill(fill);

            _pages.Add(page);

            return page;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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

        ~AddressTable()
        {
            Dispose(false);
        }
    }
}
