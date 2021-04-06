using ARMeilleure.CodeGen.Unwinding;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ARMeilleure.Translation.Cache
{
    readonly struct CacheEntry : IComparable<CacheEntry>
    {
        public readonly int Offset;
        public readonly int Size;

        public UnwindInfo UnwindInfo { get; }

        public CacheEntry(int offset, int size, UnwindInfo unwindInfo)
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