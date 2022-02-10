namespace ARMeilleure.Decoders
{
    class OpCodeT16ShiftReg : OpCodeT16, IOpCode32AluRsReg
    {
        public int Rm { get; }
        public int Rs { get; }
        public int Rd { get; }

        public int Rn { get; }

        public ShiftType ShiftType { get; }

        public bool SetFlags { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeT16ShiftReg(inst, address, opCode, inITBlock);

        public OpCodeT16ShiftReg(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rd = (opCode >> 0) & 7;
            Rm = (opCode >> 0) & 7;
            Rn = (opCode >> 3) & 7;
            Rs = (opCode >> 3) & 7;

            ShiftType = (ShiftType)(((opCode >> 6) & 1) | ((opCode >> 7) & 2));

            SetFlags = !inITBlock;
        }
    }
}
