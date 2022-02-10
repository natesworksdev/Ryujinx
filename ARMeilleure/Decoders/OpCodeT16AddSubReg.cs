namespace ARMeilleure.Decoders
{
    class OpCodeT16AddSubReg : OpCodeT16, IOpCode32AluReg
    {
        public int Rm { get; }
        public int Rd { get; }
        public int Rn { get; }

        public bool SetFlags { get; }

        public static new OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeT16AddSubReg(inst, address, opCode, inITBlock);

        public OpCodeT16AddSubReg(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rd = (opCode >> 0) & 0x7;
            Rn = (opCode >> 3) & 0x7;
            Rm = (opCode >> 6) & 0x7;

            SetFlags = !inITBlock;
        }
    }
}