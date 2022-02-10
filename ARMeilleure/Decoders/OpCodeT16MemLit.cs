namespace ARMeilleure.Decoders
{
    class OpCodeT16MemLit : OpCodeT16, IOpCode32Mem
    {
        public int Rt { get; }
        public int Rn => 15;

        public bool WBack => false;
        public bool IsLoad => true;
        public bool Index => true;
        public bool Add => true;

        public int Immediate { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeT16MemLit(inst, address, opCode, inITBlock);
        public OpCodeT16MemLit(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rt = (opCode >> 8) & 7;

            Immediate = (opCode & 0xff) << 2;
        }
    }
}