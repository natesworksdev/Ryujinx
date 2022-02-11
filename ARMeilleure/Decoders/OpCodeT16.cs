namespace ARMeilleure.Decoders
{
    class OpCodeT16 : OpCode32
    {
        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeT16(inst, address, opCode, inITBlock);

        public OpCodeT16(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Cond = Condition.Al;

            OpCodeSizeInBytes = 2;
        }
    }
}