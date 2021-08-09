﻿using System;
using System.Collections.Generic;

namespace ARMeilleure.Common
{
    unsafe class ArenaAllocator : Allocator
    {
        private class PageInfo
        {
            public IntPtr Pointer;
            public int LastUse;
        }

        private int _index;
        private int _pageIndex;
        private List<PageInfo> _pages;
        private readonly int _pageSize;
        private readonly int _pageCount;
        private readonly List<IntPtr> _extras;

        public ArenaAllocator(int pageSize, int pageCount)
        {
            _index = 0;
            _pageIndex = 0;
            _pages = new List<PageInfo>();
            _pageSize = pageSize;
            _pageCount = pageCount;
            _extras = new List<IntPtr>();
        }

        public Span<T> AllocateSpan<T>(int count) where T : unmanaged
        {
            return new Span<T>(Allocate<T>(count), count);
        }

        public override void* Allocate(int size)
        {
            if (size > _pageSize)
            {
                void* extra = NativeAllocator.Instance.Allocate(size);

                _extras.Add((IntPtr)extra);

                return extra;
            }

            if (_index + size > _pageSize)
            {
                _index = 0;
                _pageIndex++;
            }

            PageInfo info;

            if (_pageIndex < _pages.Count)
            {
                info = _pages[_pageIndex];
            }
            else
            {
                info = new PageInfo();
                info.Pointer = (IntPtr)NativeAllocator.Instance.Allocate(_pageSize);

                _pages.Add(info);
            }

            info.LastUse = Environment.TickCount;

            byte* page = (byte*)info.Pointer;
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
                NativeAllocator.Instance.Free((void*)_pages[_pages.Count - 1].Pointer);

                _pages.RemoveAt(_pages.Count - 1);
            }

            // Free extra blocks that are not page-sized
            foreach (IntPtr ptr in _extras)
            {
                NativeAllocator.Instance.Free((void*)ptr);
            }

            _extras.Clear();

            int currentTime = Environment.TickCount;

            // Free pooled pages that has not been used in a while. Remove pages at the back first, because we try to
            // keep the pages at the front alive, since they're more likely to be hot and in the d-cache.
            for (int i = _pages.Count - 1; i >= 0; i--)
            {
                PageInfo info = _pages[i];

                if (currentTime - info.LastUse >= 5000)
                {
                    NativeAllocator.Instance.Free((void*)info.Pointer);

                    _pages.RemoveAt(i);
                }
                else
                {
                    break;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_pages != null)
            {
                foreach (PageInfo info in _pages)
                {
                    NativeAllocator.Instance.Free((void*)info.Pointer);
                }

                foreach (IntPtr ptr in _extras)
                {
                    NativeAllocator.Instance.Free((void*)ptr);
                }

                _pages = null;
            }
        }

        ~ArenaAllocator()
        {
            Dispose(false);
        }
    }
}
