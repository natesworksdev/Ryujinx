namespace ARMeilleure.Decoders
{
    class OpCodeT16BImm11 : OpCode32, IOpCode32BImm
    {
        public long Immediate { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeT16BImm11(inst, address, opCode, inITBlock);

        public OpCodeT16BImm11(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            int imm = (opCode << 21) >> 20; 
            Immediate = GetPc() + imm;
        }
    }
}