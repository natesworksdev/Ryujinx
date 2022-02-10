namespace ARMeilleure.Decoders
{
    class OpCodeT16ShiftImm : OpCodeT16, IOpCode32AluRsImm
    {
        public int Rd { get; }
        public int Rn { get; }
        public int Rm { get; }

        public int Immediate { get; }
        public ShiftType ShiftType { get; }

        public bool SetFlags { get; }

        public static new OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeT16ShiftImm(inst, address, opCode, inITBlock);

        public OpCodeT16ShiftImm(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rd        = (opCode >> 0) & 0x7;
            Rm        = (opCode >> 3) & 0x7;
            Immediate = (opCode >> 6) & 0x1F;
            ShiftType = (ShiftType)((opCode >> 11) & 3);

            SetFlags = !inITBlock;
        }
    }
}
