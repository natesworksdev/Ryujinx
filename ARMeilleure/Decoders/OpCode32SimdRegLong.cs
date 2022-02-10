namespace ARMeilleure.Decoders
{
    class OpCode32SimdRegLong : OpCode32SimdReg
    {
        public bool Polynomial { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32SimdRegLong(inst, address, opCode, inITBlock);

        public OpCode32SimdRegLong(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Q = false;
            RegisterSize = RegisterSize.Simd64;

            Polynomial = ((opCode >> 9) & 0x1) != 0;

            // Subclasses have their own handling of Vx to account for before checking.
            if (GetType() == typeof(OpCode32SimdRegLong) && DecoderHelper.VectorArgumentsInvalid(true, Vd))
            {
                Instruction = InstDescriptor.Undefined;
            }
        }
    }
}
