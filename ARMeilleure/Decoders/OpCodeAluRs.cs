namespace ARMeilleure.Decoders
{
    class OpCodeAluRs : OpCodeAlu, IOpCodeAluRs
    {
        public int Shift { get; }
        public int Rm    { get; }

        public ShiftType ShiftType { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeAluRs(inst, address, opCode, inITBlock);

        public OpCodeAluRs(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            int shift = (opCode >> 10) & 0x3f;

            if (shift >= GetBitsCount())
            {
                Instruction = InstDescriptor.Undefined;

                return;
            }

            Shift = shift;

            Rm        =             (opCode >> 16) & 0x1f;
            ShiftType = (ShiftType)((opCode >> 22) & 0x3);
        }
    }
}