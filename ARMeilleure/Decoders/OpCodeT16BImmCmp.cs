namespace ARMeilleure.Decoders
{
    class OpCodeT16BImmCmp : OpCodeT16
    {
        public int Rn { get; }

        public int Immediate { get; }

        public static new OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeT16BImmCmp(inst, address, opCode, inITBlock);

        public OpCodeT16BImmCmp(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rn = (opCode >> 0) & 0x7;

            int imm = ((opCode >> 2) & 0x3e) | ((opCode >> 3) & 0x40);
            Immediate = (int)GetPc() + imm;
        }
    }
}