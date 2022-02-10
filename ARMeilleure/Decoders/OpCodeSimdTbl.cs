namespace ARMeilleure.Decoders
{
    class OpCodeSimdTbl : OpCodeSimdReg
    {
        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeSimdTbl(inst, address, opCode, inITBlock);

        public OpCodeSimdTbl(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Size = ((opCode >> 13) & 3) + 1;
        }
    }
}