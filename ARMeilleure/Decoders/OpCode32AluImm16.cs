namespace ARMeilleure.Decoders
{
    class OpCode32AluImm16 : OpCode32Alu
    {
        public int Immediate { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32AluImm16(inst, address, opCode, inITBlock);

        public OpCode32AluImm16(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            int imm12 = opCode & 0xfff;
            int imm4 = (opCode >> 16) & 0xf;

            Immediate = (imm4 << 12) | imm12;
        }
    }
}
