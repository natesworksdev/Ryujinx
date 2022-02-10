namespace ARMeilleure.Decoders
{
    class OpCode32BReg : OpCode32, IOpCode32BReg
    {
        public int Rm { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32BReg(inst, address, opCode, inITBlock);

        public OpCode32BReg(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rm = opCode & 0xf;
        }
    }
}