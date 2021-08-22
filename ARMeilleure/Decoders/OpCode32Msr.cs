namespace ARMeilleure.Decoders
{
    class OpCode32Msr : OpCode32
    {
        public int  Opc    { get; }
        public int  Mask   { get; }
        public int  Rd     { get; }
        public bool Banked { get; }
        public int  Rn     { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode) => new OpCode32Msr(inst, address, opCode);

        public OpCode32Msr(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Opc = (opCode >> 21) & 7;
            Mask = (opCode >> 16) & 0xf;
            Rd = (opCode >> 12) & 0xf;
            Banked = ((opCode >> 9) & 1) != 0;
            Rn = (opCode >> 0) & 0xf;
        }
    }
}
