namespace ARMeilleure.Decoders
{
    class OpCode32AluReg : OpCode32Alu, IOpCode32AluReg
    {
        public int Rm { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32AluReg(inst, address, opCode, inITBlock);

        public OpCode32AluReg(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rm = (opCode >> 0) & 0xf;
        }
    }
}
