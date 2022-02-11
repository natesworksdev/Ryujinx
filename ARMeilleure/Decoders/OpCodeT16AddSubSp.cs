namespace ARMeilleure.Decoders
{
    class OpCodeT16AddSubSp : OpCodeT16, IOpCode32AluImm
    {
        public int Rd => 13;
        public int Rn => 13;

        public bool SetFlags => false;

        public int Immediate { get; }

        public bool IsRotated => false;

        public static new OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeT16AddSubSp(inst, address, opCode, inITBlock);

        public OpCodeT16AddSubSp(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Immediate = (opCode >> 0) & 0x7f;
        }
    }
}