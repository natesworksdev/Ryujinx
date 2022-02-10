namespace ARMeilleure.Decoders
{
    class OpCodeSimdExt : OpCodeSimdReg
    {
        public int Imm4 { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeSimdExt(inst, address, opCode, inITBlock);

        public OpCodeSimdExt(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Imm4 = (opCode >> 11) & 0xf;
        }
    }
}