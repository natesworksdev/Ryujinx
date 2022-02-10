namespace ARMeilleure.Decoders
{
    class OpCode32AluBf : OpCode32, IOpCode32AluBf
    {
        public int Rd { get; }
        public int Rn { get; }

        public int Msb { get; }

        public int Lsb { get; }

        public int SourceMask => (int)(0xFFFFFFFF >> (31 - Msb));
        public int DestMask => SourceMask & (int)(0xFFFFFFFF << Lsb);

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32AluBf(inst, address, opCode, inITBlock);

        public OpCode32AluBf(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rd = (opCode >> 12) & 0xf;
            Rn = (opCode >> 0) & 0xf;

            Msb = (opCode >> 16) & 0x1f;
            Lsb = (opCode >> 7) & 0x1f;
        }
    }
}
