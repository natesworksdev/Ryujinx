namespace ARMeilleure.Decoders
{
    class OpCodeAluBinary : OpCodeAlu
    {
        public int Rm { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeAluBinary(inst, address, opCode, inITBlock);

        public OpCodeAluBinary(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rm = (opCode >> 16) & 0x1f;
        }
    }
}