namespace ARMeilleure.Decoders
{
    class OpCode32MsrReg : OpCode32
    {
        public int  Opc    { get; }
        public int  Mask   { get; }
        public int  Rd     { get; }
        public bool Banked { get; }
        public int  Rn     { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode) => new OpCode32MsrReg(inst, address, opCode);

        public OpCode32MsrReg(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Opc = (opCode >> 21) & 3;
            Mask = (opCode >> 16) & 0xf;
            Rd = (opCode >> 12) & 0xf;
            Banked = ((opCode >> 9) & 1) != 0;
            Rn = (opCode >> 0) & 0xf;
        }
    }
}
