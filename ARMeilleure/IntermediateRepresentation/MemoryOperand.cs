namespace ARMeilleure.IntermediateRepresentation
{
    struct MemoryOperand
    {
        public Operand? BaseAddress { get; }
        public Operand? Index       { get; }

        public Multiplier Scale { get; }

        public int Displacement { get; }

        public MemoryOperand(
            Operand?    baseAddress,
            Operand?    index        = null,
            Multiplier  scale        = Multiplier.x1,
            int         displacement = 0)
        {
            BaseAddress  = baseAddress;
            Index        = index;
            Scale        = scale;
            Displacement = displacement;
        }
    }
}