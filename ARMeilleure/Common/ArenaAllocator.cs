using System;
using System.Collections.Generic;

namespace ARMeilleure.Common
{
    unsafe class ArenaAllocator : Allocator
    {
        private int _index;
        private int _pageIndex;
        private List<IntPtr> _pages;
        private readonly int _pageSize;
        private readonly int _pageCount;

        public ArenaAllocator(int pageSize, int pageCount)
        {
            _index = 0;
            _pageIndex = 0;
            _pages = new List<IntPtr>();
            _pageSize = pageSize;
            _pageCount = pageCount;
        }

        public override void* Allocate(int size)
        {
            if (size > _pageSize)
            {
                ThrowOutOfMemory();
            }

            if (_index + size > _pageSize)
            {
                _index = 0;
                _pageIndex++;
            }

            byte* page;

            if (_pageIndex < _pages.Count)
            {
                page = (byte*)_pages[_pageIndex];
            }
            else
            {
                page = (byte*)NativeAllocator.Instance.Allocate(_pageSize);

                if (page == null)
                {
                    ThrowOutOfMemory();
                }

                _pages.Add((nint)page);
            }

            byte* result = &page[_index];

            _index += size;

            return result;
        }

        public override void Free(void* block) { }

        public void Reset()
        {
            _index = 0;
            _pageIndex = 0;

            // Free excess pages that was allocated.
            while (_pages.Count > _pageCount)
            {
                NativeAllocator.Instance.Free((void*)_pages[_pages.Count - 1]);

                _pages.RemoveAt(_pages.Count - 1);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_pages != null)
            {
                foreach (nint page in _pages)
                {
                    NativeAllocator.Instance.Free((void*)page);
                }

                _pages = null;
            }
        }

        ~ArenaAllocator()
        {
            Dispose(false);
        }

        private static void ThrowOutOfMemory() => throw new OutOfMemoryException();
    }
}
