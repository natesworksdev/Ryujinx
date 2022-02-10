namespace ARMeilleure.Decoders
{
    class OpCodeSimdCvt : OpCodeSimd
    {
        public int FBits { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeSimdCvt(inst, address, opCode, inITBlock);

        public OpCodeSimdCvt(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            int scale = (opCode >> 10) & 0x3f;
            int sf    = (opCode >> 31) & 0x1;

            FBits = 64 - scale;

            RegisterSize = sf != 0
                ? RegisterSize.Int64
                : RegisterSize.Int32;
        }
    }
}