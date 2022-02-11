namespace ARMeilleure.Decoders
{
    class OpCodeT16Exception : OpCodeT16, IOpCode32Exception
    {
        public int Id { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeT16Exception(inst, address, opCode, inITBlock);

        public OpCodeT16Exception(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Id = opCode & 0xFF;
        }
    }
}
