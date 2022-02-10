namespace ARMeilleure.Decoders
{
    class OpCodeT16AluImm8 : OpCodeT16, IOpCode32AluImm
    {
        public int Rd { get; }
        public int Rn { get; }

        public bool SetFlags { get; }

        public int Immediate { get; }

        public bool IsRotated { get; }

        public static new OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeT16AluImm8(inst, address, opCode, inITBlock);

        public OpCodeT16AluImm8(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rd = (opCode >> 8) & 0x7;
            Rn = (opCode >> 8) & 0x7;
            Immediate = (opCode >> 0) & 0xff;
            IsRotated = false;

            SetFlags = !inITBlock;
        }
    }
}