namespace ARMeilleure.CodeGen.Unwinding
{
    readonly struct UnwindPushEntry
    {
        public const int Stride = 16; // Bytes.

        public readonly UnwindPseudoOp PseudoOp;
        public readonly int PrologOffset;
        public readonly int RegIndex;
        public readonly int StackOffsetOrAllocSize;

        public UnwindPushEntry(UnwindPseudoOp pseudoOp, int prologOffset, int regIndex = -1, int stackOffsetOrAllocSize = -1)
        {
            PseudoOp = pseudoOp;
            PrologOffset = prologOffset;
            RegIndex = regIndex;
            StackOffsetOrAllocSize = stackOffsetOrAllocSize;
        }
    }
}