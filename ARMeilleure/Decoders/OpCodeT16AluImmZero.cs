namespace ARMeilleure.Decoders
{
    class OpCodeT16AluImmZero : OpCodeT16, IOpCode32AluImm
    {
        public int Rd { get; }
        public int Rn { get; }

        public bool SetFlags { get; }

        public int Immediate { get; }

        public bool IsRotated { get; }

        public static new OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeT16AluImmZero(inst, address, opCode, inITBlock);

        public OpCodeT16AluImmZero(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rd = (opCode >> 0) & 0x7;
            Rn = (opCode >> 3) & 0x7;
            Immediate = 0;
            IsRotated = false;

            SetFlags = !inITBlock;
        }
    }
}