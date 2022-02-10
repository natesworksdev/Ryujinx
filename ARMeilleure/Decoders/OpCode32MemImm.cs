namespace ARMeilleure.Decoders
{
    class OpCode32MemImm : OpCode32Mem
    {
        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32MemImm(inst, address, opCode, inITBlock);

        public OpCode32MemImm(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Immediate = opCode & 0xfff;
        }
    }
}