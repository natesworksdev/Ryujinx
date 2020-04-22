using ARMeilleure.IntermediateRepresentation;

namespace ARMeilleure.CodeGen.Unwinding
{
    readonly struct UnwindPushEntry
    {
        public readonly int Index { get; }

        public readonly RegisterType Type { get; }

        public readonly int StreamEndOffset { get; }

        public UnwindPushEntry(int index, RegisterType type, int streamEndOffset)
        {
            Index           = index;
            Type            = type;
            StreamEndOffset = streamEndOffset;
        }
    }
}