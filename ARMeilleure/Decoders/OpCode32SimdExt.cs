namespace ARMeilleure.Decoders
{
    class OpCode32SimdExt : OpCode32SimdReg
    {
        public int Immediate { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32SimdExt(inst, address, opCode, inITBlock);

        public OpCode32SimdExt(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Immediate = (opCode >> 8) & 0xf;
            Size = 0;
            if (DecoderHelper.VectorArgumentsInvalid(Q, Vd, Vm, Vn) || (!Q && Immediate > 7))
            {
                Instruction = InstDescriptor.Undefined;
            }
        }
    }
}
