using System;

namespace ARMeilleure.CodeGen.RegisterAllocators
{
    readonly struct LiveRange : IComparable<LiveRange>
    {
        public readonly int Start;
        public readonly int End;

        public LiveRange(int start, int end)
        {
            Start = start;
            End   = end;
        }

        public int CompareTo(LiveRange other)
        {
            if (Start < other.End && other.Start < End)
            {
                return 0;
            }

            return Start.CompareTo(other.Start);
        }

        public override string ToString()
        {
            return $"[{Start}, {End}[";
        }
    }
}