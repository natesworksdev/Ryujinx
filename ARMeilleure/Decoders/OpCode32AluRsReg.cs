namespace ARMeilleure.Decoders
{
    class OpCode32AluRsReg : OpCode32Alu, IOpCode32AluRsReg
    {
        public int Rm { get; }
        public int Rs { get; }

        public ShiftType ShiftType { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32AluRsReg(inst, address, opCode, inITBlock);

        public OpCode32AluRsReg(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rm = (opCode >> 0) & 0xf;
            Rs = (opCode >> 8) & 0xf;

            ShiftType = (ShiftType)((opCode >> 5) & 3);
        }
    }
}
