using ARMeilleure.CodeGen;
using ARMeilleure.Memory;
using System;
using System.Collections.Generic;

namespace ARMeilleure.Translation
{
    static class JitCache
    {
        private const int PageSize = 4 * 1024;
        private const int PageMask = PageSize - 1;

        private static uint CacheSize = 512 * 1024 * 1024;

        private static IntPtr _basePointer;

        private static int _offset;

        private static List<JitCacheEntry> _cacheEntries;

        static JitCache()
        {
            _basePointer = MemoryManagement.Allocate(CacheSize);

            JitUnwindWindows.InstallFunctionTableHandler(_basePointer, CacheSize);

            //The first page is used for the table based SEH structs.
            _offset = PageSize;

            _cacheEntries = new List<JitCacheEntry>();
        }

        public static IntPtr Map(CompiledFunction func)
        {
            byte[] code = func.Code;

            int funcOffset = Allocate(code.Length);

            IntPtr funcPtr = _basePointer + funcOffset;

            unsafe
            {
                fixed (byte* codePtr = code)
                {
                    byte* dest = (byte*)funcPtr;

                    long size = (long)code.Length;

                    Buffer.MemoryCopy(codePtr, dest, size, size);
                }
            }

            //TODO: W^X.
            MemoryManagement.Reprotect(funcPtr, (ulong)code.Length, MemoryProtection.ReadWriteExecute);

            Add(new JitCacheEntry(funcOffset, code.Length, func.UnwindInfo));

            return funcPtr;
        }

        private static int Allocate(int codeSize)
        {
            int allocOffset = _offset;

            _offset += codeSize;

            if ((ulong)(uint)_offset > CacheSize)
            {
                throw new OutOfMemoryException();
            }

            return allocOffset;
        }

        private static void Add(JitCacheEntry entry)
        {
            //TODO: Use concurrent collection.
            _cacheEntries.Add(entry);
        }

        public static bool TryFind(int offset, out JitCacheEntry entry)
        {
            foreach (JitCacheEntry cacheEntry in _cacheEntries)
            {
                int endOffset = cacheEntry.Offset + cacheEntry.Size;

                if (offset >= cacheEntry.Offset && offset < endOffset)
                {
                    entry = cacheEntry;

                    return true;
                }
            }

            entry = default(JitCacheEntry);

            return false;
        }
    }
}