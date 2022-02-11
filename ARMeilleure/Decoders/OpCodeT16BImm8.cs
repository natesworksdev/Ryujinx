namespace ARMeilleure.Decoders
{
    class OpCodeT16BImm8 : OpCode32, IOpCode32BImm
    {
        public long Immediate { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeT16BImm8(inst, address, opCode, inITBlock);

        public OpCodeT16BImm8(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Cond = (Condition)((opCode >> 8) & 0xf);

            int imm = (opCode << 24) >> 23; 
            Immediate = GetPc() + imm;
        }
    }
}