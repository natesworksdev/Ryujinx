using System.Runtime.InteropServices;

namespace ARMeilleure.CodeGen.Unwinding
{
    [StructLayout(LayoutKind.Sequential, Pack = 1/*, Size = 16*/)]
    struct UnwindPushEntry
    {
        public UnwindPseudoOp PseudoOp { get; }
        public int PrologOffset { get; }
        public int RegIndex { get; }
        public int StackOffsetOrAllocSize { get; }

        public UnwindPushEntry(UnwindPseudoOp pseudoOp, int prologOffset, int regIndex = -1, int stackOffsetOrAllocSize = -1)
        {
            PseudoOp = pseudoOp;
            PrologOffset = prologOffset;
            RegIndex = regIndex;
            StackOffsetOrAllocSize = stackOffsetOrAllocSize;
        }
    }
}