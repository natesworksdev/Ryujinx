namespace ARMeilleure.Decoders
{
    class OpCodeT16MemSp : OpCodeT16, IOpCode32Mem
    {
        public int Rt { get; }
        public int Rn => 13;

        public bool WBack => false;
        public bool IsLoad { get; }
        public bool Index => true;
        public bool Add => true;

        public int Immediate { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeT16MemSp(inst, address, opCode, inITBlock);

        public OpCodeT16MemSp(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rt = (opCode >> 8) & 7;

            IsLoad = ((opCode >> 11) & 1) != 0;

            Immediate = ((opCode >> 0) & 0xff) << 2;
        }
    }
}