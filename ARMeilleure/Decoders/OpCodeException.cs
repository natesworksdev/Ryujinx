namespace ARMeilleure.Decoders
{
    class OpCodeException : OpCode
    {
        public int Id { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeException(inst, address, opCode, inITBlock);

        public OpCodeException(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Id = (opCode >> 5) & 0xffff;
        }
    }
}