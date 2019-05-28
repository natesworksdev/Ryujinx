using ARMeilleure.IntermediateRepresentation;

namespace ARMeilleure.CodeGen.X86
{
    class X86MemoryOperand : Operand
    {
        public Operand BaseAddress { get; set; }
        public Operand Index       { get; set; }
        public Scale   Scale       { get; }

        public int Displacement { get; }

        public X86MemoryOperand(
            OperandType type,
            Operand     baseAddress,
            Operand     index,
            Scale       scale,
            int         displacement) : base(OperandKind.Memory, type)
        {
            BaseAddress  = baseAddress;
            Index        = index;
            Scale        = scale;
            Displacement = displacement;
        }
    }
}