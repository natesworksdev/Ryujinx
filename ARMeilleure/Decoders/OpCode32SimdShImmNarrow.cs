namespace ARMeilleure.Decoders
{
    class OpCode32SimdShImmNarrow : OpCode32SimdShImm
    {
        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32SimdShImmNarrow(inst, address, opCode, inITBlock);

        public OpCode32SimdShImmNarrow(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock) { }
    }
}
