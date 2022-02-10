namespace ARMeilleure.Decoders
{
    class OpCodeSimdReg : OpCodeSimd
    {
        public bool Bit3 { get; }
        public int  Ra   { get; }
        public int  Rm   { get; protected set; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeSimdReg(inst, address, opCode, inITBlock);

        public OpCodeSimdReg(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Bit3 = ((opCode >>  3) & 0x1) != 0;
            Ra   =  (opCode >> 10) & 0x1f;
            Rm   =  (opCode >> 16) & 0x1f;
        }
    }
}