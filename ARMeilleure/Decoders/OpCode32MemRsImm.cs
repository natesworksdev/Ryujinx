namespace ARMeilleure.Decoders
{
    class OpCode32MemRsImm : OpCode32Mem
    {
        public int Rm { get; }
        public ShiftType ShiftType { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32MemRsImm(inst, address, opCode, inITBlock);

        public OpCode32MemRsImm(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rm = (opCode >> 0) & 0xf;
            Immediate = (opCode >> 7) & 0x1f;

            ShiftType = (ShiftType)((opCode >> 5) & 3);
        }
    }
}
