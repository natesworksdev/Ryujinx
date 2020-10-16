namespace ARMeilleure.Decoders
{
    class OpCode32SimdSpecial : OpCode32
    {
        public int Rt { get; private set; }
        public int Sreg { get; private set; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode) => new OpCode32SimdSpecial(inst, address, opCode);

        public OpCode32SimdSpecial(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Rt = (opCode >> 12) & 0xf;
            Sreg = (opCode >> 16) & 0xf;
        }
    }
}
