namespace ARMeilleure.Decoders
{
    class OpCodeSimdMemPair : OpCodeMemPair, IOpCodeSimd
    {
        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeSimdMemPair(inst, address, opCode, inITBlock);

        public OpCodeSimdMemPair(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Size = ((opCode >> 30) & 3) + 2;

            Extend64 = false;

            DecodeImm(opCode);
        }
    }
}