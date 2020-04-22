namespace ARMeilleure.CodeGen.Unwinding
{
    readonly struct UnwindInfo
    {
        public readonly UnwindPushEntry[] PushEntries { get; }

        public readonly int PrologueSize { get; }

        public readonly int FixedAllocSize { get; }

        public UnwindInfo(UnwindPushEntry[] pushEntries, int prologueSize, int fixedAllocSize)
        {
            PushEntries    = pushEntries;
            PrologueSize   = prologueSize;
            FixedAllocSize = fixedAllocSize;
        }
    }
}