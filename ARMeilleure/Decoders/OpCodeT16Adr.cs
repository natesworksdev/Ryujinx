namespace ARMeilleure.Decoders
{
    class OpCodeT16Adr : OpCodeT16, IOpCode32Adr
    {
        public int Rd { get; }

        public bool Add => true;
        public int Immediate { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeT16Adr(inst, address, opCode, inITBlock);

        public OpCodeT16Adr(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rd  = (opCode >> 8) & 7;

            int imm = (opCode & 0xff) << 2;
            Immediate = (int)(GetPc() & 0xfffffffc) + imm;
        }
    }
}
