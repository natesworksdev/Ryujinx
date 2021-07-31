using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ARMeilleure.Common
{
    unsafe class Arena
    {
        [ThreadStatic]
        private static List<Arena> _instances;

        private static List<Arena> Instances
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
        private readonly List<nint> _pages;

        public Arena()
        {
            _index = 0;
            _pageIndex = 0;
            _pages = new List<nint>();

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
                page = (byte*)Marshal.AllocHGlobal(PageSize);

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

        public void Reset()
        {
            _index = 0;
            _pageIndex = 0;

            // Free excess pages that was allocated.
            while (_pages.Count > PageCount)
            {
                Marshal.FreeHGlobal(_pages[_pages.Count - 1]);

                _pages.RemoveAt(_pages.Count - 1);
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

    unsafe class Arena<T> : Arena where T : unmanaged
    {
        [ThreadStatic]
        private static Arena<T> _instance;

        private static Arena<T> Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Arena<T>();
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
