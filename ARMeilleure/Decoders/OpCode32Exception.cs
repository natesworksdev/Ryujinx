namespace ARMeilleure.Decoders
{
    class OpCode32Exception : OpCode32
    {
        public int Id { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32Exception(inst, address, opCode, inITBlock);

        public OpCode32Exception(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Id = opCode & 0xFFFFFF;
        }
    }
}
