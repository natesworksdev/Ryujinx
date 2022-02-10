namespace ARMeilleure.Decoders
{
    class OpCodeBImm : OpCode, IOpCodeBImm
    {
        public long Immediate { get; protected set; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeBImm(inst, address, opCode, inITBlock);

        public OpCodeBImm(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock) { }
    }
}