using System;
using System.Collections.Generic;

namespace ARMeilleure.Common
{
    unsafe class ArenaAllocator : IAllocator
    {
        [ThreadStatic]
        private static List<ArenaAllocator> _instances;

        private static List<ArenaAllocator> Instances
        {
            get
            {
                if (_instances == null)
                {
                    _instances = new(capacity: 4);
                }

                return _instances;
            }
        }

        private const int PageCount = 32;
        private const int PageSize = 256 * 1024;

        private int _index;
        private int _pageIndex;
        private List<IntPtr> _pages;

        public ArenaAllocator()
        {
            _index = 0;
            _pageIndex = 0;
            _pages = new List<IntPtr>();

            Instances.Add(this);
        }

        public void* Allocate(int size)
        {
            if (size > PageSize)
            {
                ThrowOutOfMemory();
            }

            if (_index + size > PageSize)
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
                page = (byte*)NativeAllocator.Instance.Allocate(PageSize);

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

        public void Free(void* block) { }

        public void Reset()
        {
            _index = 0;
            _pageIndex = 0;

            // Free excess pages that was allocated.
            while (_pages.Count > PageCount)
            {
                NativeAllocator.Instance.Free((void*)_pages[_pages.Count - 1]);

                _pages.RemoveAt(_pages.Count - 1);
            }
        }

        public void Dispose()
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

        public static void ResetAll()
        {
            foreach (var instance in Instances)
            {
                instance.Reset();
            }
        }

        private static void ThrowOutOfMemory() => throw new OutOfMemoryException();
    }

    unsafe class ArenaAllocator<T> : ArenaAllocator where T : unmanaged
    {
        [ThreadStatic]
        private static ArenaAllocator<T> _instance;

        public static ArenaAllocator<T> Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ArenaAllocator<T>();
                }
                
                return _instance;
            }
        }

        public static T* Alloc(int count = 1)
        {
            return (T*)Instance.Allocate(count * sizeof(T));
        }
    }
}
