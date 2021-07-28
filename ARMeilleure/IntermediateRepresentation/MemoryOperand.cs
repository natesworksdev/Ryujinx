namespace ARMeilleure.IntermediateRepresentation
{
    struct MemoryOperand
    {
        public Operand BaseAddress { get; set; }
        public Operand Index { get; set; }
        public Multiplier Scale { get; set; }
        public int Displacement { get; set; }
    }
}