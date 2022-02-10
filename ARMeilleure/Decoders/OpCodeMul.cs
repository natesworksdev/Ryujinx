namespace ARMeilleure.Decoders
{
    class OpCodeMul : OpCodeAlu
    {
        public int Rm { get; }
        public int Ra { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeMul(inst, address, opCode, inITBlock);

        public OpCodeMul(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Ra = (opCode >> 10) & 0x1f;
            Rm = (opCode >> 16) & 0x1f;
        }
    }
}