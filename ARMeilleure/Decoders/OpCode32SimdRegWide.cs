namespace ARMeilleure.Decoders
{
    class OpCode32SimdRegWide : OpCode32SimdReg
    {
        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32SimdRegWide(inst, address, opCode, inITBlock);

        public OpCode32SimdRegWide(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Q = false;
            RegisterSize = RegisterSize.Simd64;

            // Subclasses have their own handling of Vx to account for before checking.
            if (GetType() == typeof(OpCode32SimdRegWide) && DecoderHelper.VectorArgumentsInvalid(true, Vd, Vn))
            {
                Instruction = InstDescriptor.Undefined;
            }
        }
    }
}
