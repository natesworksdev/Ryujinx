using ARMeilleure.CodeGen;
using ARMeilleure.CodeGen.Unwinding;
using ARMeilleure.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ARMeilleure.Translation.Cache
{
    static class JitCache
    {
        private const uint PageSize = 4 * 1024;
        private const uint PageMask = PageSize - 1;

        private const uint CodeAlignment = 4; // Bytes.
        private const uint CacheSize = 3U * 1024 * 1024 * 1024; // Default cache size = 2047 * 1024 * 1024.

        private static ReservedRegion _jitRegion;

        private static CacheMemoryAllocator _cacheAllocator;

        private static readonly List<CacheEntry> _cacheEntries = new List<CacheEntry>();

        private static readonly object _lock = new object();
        private static bool _initialized;

        public static void Initialize(IJitMemoryAllocator allocator)
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                _jitRegion = new ReservedRegion(allocator, CacheSize);

                _cacheAllocator = new CacheMemoryAllocator(CacheSize);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    JitUnwindWindows.InstallFunctionTableHandler(_jitRegion.Pointer, CacheSize, new IntPtr(_jitRegion.Pointer.ToInt64() + Allocate(PageSize)));
                }

                _initialized = true;
            }
        }

        public static IntPtr Map(CompiledFunction func)
        {
            byte[] code = func.Code;

            lock (_lock)
            {
                Debug.Assert(_initialized);

                uint funcOffset = Allocate((uint)code.Length);

                IntPtr funcPtr = new IntPtr(_jitRegion.Pointer.ToInt64() + funcOffset);

                ReprotectAsWritable(funcOffset, (uint)code.Length);

                Marshal.Copy(code, 0, funcPtr, code.Length);

                ReprotectAsExecutable(funcOffset, (uint)code.Length);

                Add(funcOffset, (uint)code.Length, func.UnwindInfo);

                return funcPtr;
            }
        }

        public static void Unmap(IntPtr pointer)
        {
            lock (_lock)
            {
                Debug.Assert(_initialized);

                uint funcOffset = (uint)(pointer.ToInt64() - _jitRegion.Pointer.ToInt64());

                bool result = TryFind(funcOffset, out CacheEntry entry);
                Debug.Assert(result);

                _cacheAllocator.Free(funcOffset, AlignCodeSize(entry.Size));

                Remove(funcOffset);
            }
        }

        private static void ReprotectAsWritable(uint offset, uint size)
        {
            uint endOffs = offset + size;

            uint regionStart = offset & ~PageMask;
            uint regionEnd = (endOffs + PageMask) & ~PageMask;

            _jitRegion.Block.MapAsRwx(regionStart, regionEnd - regionStart);
        }

        private static void ReprotectAsExecutable(uint offset, uint size)
        {
            uint endOffs = offset + size;

            uint regionStart = offset & ~PageMask;
            uint regionEnd = (endOffs + PageMask) & ~PageMask;

            _jitRegion.Block.MapAsRx(regionStart, regionEnd - regionStart);
        }

        private static uint Allocate(uint codeSize)
        {
            codeSize = AlignCodeSize(codeSize);

            if (!_cacheAllocator.TryAllocate(codeSize, out uint allocOffset))
            {
                throw new OutOfMemoryException("JIT Cache exhausted.");
            }

            _jitRegion.ExpandIfNeeded(allocOffset + codeSize);

            return allocOffset;
        }

        private static uint AlignCodeSize(uint codeSize)
        {
            return (codeSize + (CodeAlignment - 1)) & ~(CodeAlignment - 1);
        }

        private static void Add(uint offset, uint size, UnwindInfo unwindInfo)
        {
            CacheEntry entry = new CacheEntry(offset, size, unwindInfo);

            int index = _cacheEntries.BinarySearch(entry);

            if (index < 0)
            {
                index = ~index;
            }

            _cacheEntries.Insert(index, entry);
        }

        private static void Remove(uint offset)
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

        public static bool TryFind(uint offset, out CacheEntry entry)
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
    }
}