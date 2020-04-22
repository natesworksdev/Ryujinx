using ARMeilleure.CodeGen.Unwinding;

namespace ARMeilleure.Translation
{
    readonly struct JitCacheEntry
    {
        public readonly int Offset { get; }
        public readonly int Size   { get; }

        public readonly UnwindInfo UnwindInfo { get; }

        public JitCacheEntry(int offset, int size, in UnwindInfo unwindInfo)
        {
            Offset     = offset;
            Size       = size;
            UnwindInfo = unwindInfo;
        }
    }
}