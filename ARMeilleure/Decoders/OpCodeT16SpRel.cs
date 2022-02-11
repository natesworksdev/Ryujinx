namespace ARMeilleure.Decoders
{
    class OpCodeT16SpRel : OpCodeT16, IOpCode32AluImm
    {
        public int Rd { get; }
        public int Rn => 13;

        public bool SetFlags => false;

        public int Immediate { get; }

        public bool IsRotated => false;

        public static new OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeT16SpRel(inst, address, opCode, inITBlock);

        public OpCodeT16SpRel(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rd = (opCode >> 8) & 0x7;
            Immediate = (opCode >> 0) & 0xff;
        }
    }
}