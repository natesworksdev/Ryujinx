namespace ARMeilleure.Decoders
{
    class OpCodeCcmpImm : OpCodeCcmp, IOpCodeAluImm
    {
        public long Immediate => RmImm;

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeCcmpImm(inst, address, opCode, inITBlock);

        public OpCodeCcmpImm(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock) { }
    }
}