namespace ARMeilleure.Decoders
{
    class OpCodeMem : OpCode
    {
        public int  Rt       { get; protected set; }
        public int  Rn       { get; protected set; }
        public int  Size     { get; protected set; }
        public bool Extend64 { get; protected set; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeMem(inst, address, opCode, inITBlock);

        public OpCodeMem(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rt   = (opCode >>  0) & 0x1f;
            Rn   = (opCode >>  5) & 0x1f;
            Size = (opCode >> 30) & 0x3;
        }
    }
}