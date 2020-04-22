using System;

namespace ARMeilleure.CodeGen.RegisterAllocators
{
    readonly struct LiveRange : IComparable<LiveRange>
    {
        public readonly int Start { get; }
        public readonly int End   { get; }

        public LiveRange(int start, int end)
        {
            Start = start;
            End   = end;
        }

        public readonly int CompareTo(LiveRange other)
        {
            if (Start < other.End && other.Start < End)
            {
                return 0;
            }

            return Start.CompareTo(other.Start);
        }

        public override readonly string ToString()
        {
            return $"[{Start}, {End}[";
        }
    }
}