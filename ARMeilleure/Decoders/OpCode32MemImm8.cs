namespace ARMeilleure.Decoders
{
    class OpCode32MemImm8 : OpCode32Mem
    {
        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32MemImm8(inst, address, opCode, inITBlock);

        public OpCode32MemImm8(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            int imm4L = (opCode >> 0) & 0xf;
            int imm4H = (opCode >> 8) & 0xf;

            Immediate = imm4L | (imm4H << 4);
        }
    }
}