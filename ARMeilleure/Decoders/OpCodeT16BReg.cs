namespace ARMeilleure.Decoders
{
    class OpCodeT16BReg : OpCodeT16, IOpCode32BReg
    {
        public int Rm { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeT16BReg(inst, address, opCode, inITBlock);

        public OpCodeT16BReg(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rm = (opCode >> 3) & 0xf;
        }
    }
}
