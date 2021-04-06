namespace ARMeilleure.CodeGen.Unwinding
{
    readonly struct UnwindInfo
    {
        public const int Stride = 4; // Bytes.

        public readonly UnwindPushEntry[] PushEntries;
        public readonly int PrologSize;

        public UnwindInfo(UnwindPushEntry[] pushEntries, int prologSize)
        {
            PushEntries = pushEntries;
            PrologSize = prologSize;
        }
    }
}