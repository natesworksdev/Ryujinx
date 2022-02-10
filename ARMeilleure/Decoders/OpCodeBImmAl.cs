namespace ARMeilleure.Decoders
{
    class OpCodeBImmAl : OpCodeBImm
    {
        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeBImmAl(inst, address, opCode, inITBlock);

        public OpCodeBImmAl(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Immediate = (long)address + DecoderHelper.DecodeImm26_2(opCode);
        }
    }
}