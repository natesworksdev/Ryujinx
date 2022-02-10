namespace ARMeilleure.Decoders
{
    class OpCodeCsel : OpCodeAlu, IOpCodeCond
    {
        public int Rm { get; }

        public Condition Cond { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeCsel(inst, address, opCode, inITBlock);

        public OpCodeCsel(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rm   =             (opCode >> 16) & 0x1f;
            Cond = (Condition)((opCode >> 12) & 0xf);
        }
    }
}