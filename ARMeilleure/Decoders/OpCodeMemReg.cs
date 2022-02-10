namespace ARMeilleure.Decoders
{
    class OpCodeMemReg : OpCodeMem
    {
        public bool Shift { get; }
        public int  Rm    { get; }

        public IntType IntType { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeMemReg(inst, address, opCode, inITBlock);

        public OpCodeMemReg(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Shift    =          ((opCode >> 12) & 0x1) != 0;
            IntType  = (IntType)((opCode >> 13) & 0x7);
            Rm       =           (opCode >> 16) & 0x1f;
            Extend64 =          ((opCode >> 22) & 0x3) == 2;
        }
    }
}