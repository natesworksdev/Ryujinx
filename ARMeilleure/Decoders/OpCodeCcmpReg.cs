namespace ARMeilleure.Decoders
{
    class OpCodeCcmpReg : OpCodeCcmp, IOpCodeAluRs
    {
        public int Rm => RmImm;

        public int Shift => 0;

        public ShiftType ShiftType => ShiftType.Lsl;

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeCcmpReg(inst, address, opCode, inITBlock);

        public OpCodeCcmpReg(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock) { }
    }
}