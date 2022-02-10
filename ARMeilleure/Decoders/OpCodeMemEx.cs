namespace ARMeilleure.Decoders
{
    class OpCodeMemEx : OpCodeMem
    {
        public int Rt2 { get; }
        public int Rs  { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeMemEx(inst, address, opCode, inITBlock);

        public OpCodeMemEx(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rt2 = (opCode >> 10) & 0x1f;
            Rs  = (opCode >> 16) & 0x1f;
        }
    }
}