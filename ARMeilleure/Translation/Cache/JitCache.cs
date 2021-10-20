using ARMeilleure.CodeGen;
using ARMeilleure.CodeGen.Unwinding;
using ARMeilleure.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ARMeilleure.Translation.Cache
{
    class JitCache : IDisposable
    {
        private const int PageSize = 4 * 1024;
        private const int PageMask = PageSize - 1;

        private const int CodeAlignment = 4; // Bytes.
        private const int CacheSize = 2047 * 1024 * 1024;

        private readonly object _lock;
        private readonly ReservedRegion _codeRegion;
        private readonly List<CacheEntry> _cacheEntries;
        private readonly CacheMemoryAllocator _cacheAllocator;
        private readonly JitUnwindWindows _jitUnwindWindows;

        public int Size { get; }
        public IntPtr Base => _codeRegion.Pointer;

        public JitCache(IJitMemoryAllocator allocator)
        {
            Size = CacheSize;

            _lock = new object();

            _codeRegion = new ReservedRegion(allocator, (uint)Size);
            _cacheEntries = new List<CacheEntry>();
            _cacheAllocator = new CacheMemoryAllocator(Size);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Allocate(PageSize);

                _jitUnwindWindows = new JitUnwindWindows(this);
            }
        }

        public IntPtr Map(CompiledFunction func)
        {
            byte[] code = func.Code;

            lock (_lock)
            {
                int funcOffset = Allocate(code.Length);

                IntPtr funcPtr = _codeRegion.Pointer + funcOffset;

                ReprotectAsWritable(funcOffset, code.Length);

                Marshal.Copy(code, 0, funcPtr, code.Length);

                ReprotectAsExecutable(funcOffset, code.Length);

                Add(funcOffset, code.Length, func.UnwindInfo);

                return funcPtr;
            }
        }

        public void Unmap(IntPtr pointer)
        {
            lock (_lock)
            {
                int funcOffset = (int)(pointer.ToInt64() - _codeRegion.Pointer.ToInt64());

                bool result = TryFind(funcOffset, out CacheEntry entry);
                Debug.Assert(result);

                _cacheAllocator.Free(funcOffset, AlignCodeSize(entry.Size));

                Remove(funcOffset);
            }
        }

        private void ReprotectAsWritable(int offset, int size)
        {
            int endOffs = offset + size;

            int regionStart = offset & ~PageMask;
            int regionEnd = (endOffs + PageMask) & ~PageMask;

            _codeRegion.Block.MapAsRwx((ulong)regionStart, (ulong)(regionEnd - regionStart));
        }

        private void ReprotectAsExecutable(int offset, int size)
        {
            int endOffs = offset + size;

            int regionStart = offset & ~PageMask;
            int regionEnd = (endOffs + PageMask) & ~PageMask;

            _codeRegion.Block.MapAsRx((ulong)regionStart, (ulong)(regionEnd - regionStart));
        }

        private int Allocate(int codeSize)
        {
            codeSize = AlignCodeSize(codeSize);

            int allocOffset = _cacheAllocator.Allocate(codeSize);

            if (allocOffset < 0)
            {
                throw new OutOfMemoryException("JIT Cache exhausted.");
            }

            _codeRegion.ExpandIfNeeded((ulong)allocOffset + (ulong)codeSize);

            return allocOffset;
        }

        private static int AlignCodeSize(int codeSize)
        {
            return checked(codeSize + (CodeAlignment - 1)) & ~(CodeAlignment - 1);
        }

        private void Add(int offset, int size, UnwindInfo unwindInfo)
        {
            CacheEntry entry = new CacheEntry(offset, size, unwindInfo);

            int index = _cacheEntries.BinarySearch(entry);

            if (index < 0)
            {
                index = ~index;
            }

            _cacheEntries.Insert(index, entry);
        }

        private void Remove(int offset)
        {
            int index = _cacheEntries.BinarySearch(new CacheEntry(offset, 0, default));

            if (index < 0)
            {
                index = ~index - 1;
            }

            if (index >= 0)
            {
                _cacheEntries.RemoveAt(index);
            }
        }

        public bool TryFind(int offset, out CacheEntry entry)
        {
            lock (_lock)
            {
                int index = _cacheEntries.BinarySearch(new CacheEntry(offset, 0, default));

                if (index < 0)
                {
                    index = ~index - 1;
                }

                if (index >= 0)
                {
                    entry = _cacheEntries[index];
                    return true;
                }
            }

            entry = default;
            return false;
        }

        public void Dispose()
        {
            _jitUnwindWindows?.Dispose();
            _codeRegion.Dispose();
        }
    }
}