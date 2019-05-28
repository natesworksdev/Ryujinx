namespace ARMeilleure.CodeGen.RegisterAllocators
{
    struct LiveRange
    {
        public int Start { get; }
        public int End   { get; }

        public LiveRange(int start, int end)
        {
            Start = start;
            End   = end;
        }

        public override string ToString()
        {
            return $"[{Start}, {End}[";
        }
    }
}