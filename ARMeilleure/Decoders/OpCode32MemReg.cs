namespace ARMeilleure.Decoders
{
    class OpCode32MemReg : OpCode32Mem, IOpCode32MemReg
    {
        public int Rm { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32MemReg(inst, address, opCode, inITBlock);

        public OpCode32MemReg(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rm = (opCode >> 0) & 0xf;
        }
    }
}
