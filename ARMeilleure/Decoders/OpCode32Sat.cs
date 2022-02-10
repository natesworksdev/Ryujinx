namespace ARMeilleure.Decoders
{
    class OpCode32Sat : OpCode32
    {
        public int Rn { get; }
        public int Imm5 { get; }
        public int Rd { get; }
        public int SatImm { get; }

        public ShiftType ShiftType { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32Sat(inst, address, opCode, inITBlock);

        public OpCode32Sat(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rn = (opCode >> 0) & 0xf;
            Imm5 = (opCode >> 7) & 0x1f;
            Rd = (opCode >> 12) & 0xf;
            SatImm = (opCode >> 16) & 0x1f;

            ShiftType = (ShiftType)((opCode >> 5) & 2);
        }
    }
}