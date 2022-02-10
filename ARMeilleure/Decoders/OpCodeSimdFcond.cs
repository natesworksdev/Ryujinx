namespace ARMeilleure.Decoders
{
    class OpCodeSimdFcond : OpCodeSimdReg, IOpCodeCond
    {
        public int Nzcv { get; }

        public Condition Cond { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeSimdFcond(inst, address, opCode, inITBlock);

        public OpCodeSimdFcond(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Nzcv =             (opCode >>  0) & 0xf;
            Cond = (Condition)((opCode >> 12) & 0xf);
        }
    }
}
