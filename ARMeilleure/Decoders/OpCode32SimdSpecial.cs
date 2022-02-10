namespace ARMeilleure.Decoders
{
    class OpCode32SimdSpecial : OpCode32
    {
        public int Rt { get; }
        public int Sreg { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32SimdSpecial(inst, address, opCode, inITBlock);

        public OpCode32SimdSpecial(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rt = (opCode >> 12) & 0xf;
            Sreg = (opCode >> 16) & 0xf;
        }
    }
}
