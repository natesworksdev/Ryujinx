using ARMeilleure.CodeGen.Unwinding;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ARMeilleure.Translation.Cache
{
    struct CacheEntry : IComparable<CacheEntry>
    {
        public uint Offset { get; }
        public uint Size   { get; }

        public UnwindInfo UnwindInfo { get; }

        public CacheEntry(uint offset, uint size, UnwindInfo unwindInfo)
        {
            Offset     = offset;
            Size       = size;
            UnwindInfo = unwindInfo;
        }

        public int CompareTo([AllowNull] CacheEntry other)
        {
            return Offset.CompareTo(other.Offset);
        }
    }
}