namespace ARMeilleure.Decoders
{
    class OpCode32Sat16 : OpCode32
    {
        public int Rn { get; }
        public int Rd { get; }
        public int SatImm { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32Sat16(inst, address, opCode, inITBlock);

        public OpCode32Sat16(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rn = (opCode >> 0) & 0xf;
            Rd = (opCode >> 12) & 0xf;
            SatImm = (opCode >> 16) & 0xf;
        }
    }
}