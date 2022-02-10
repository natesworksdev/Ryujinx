namespace ARMeilleure.Decoders
{
    class OpCodeSimdMemReg : OpCodeMemReg, IOpCodeSimd
    {
        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeSimdMemReg(inst, address, opCode, inITBlock);

        public OpCodeSimdMemReg(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Size |= (opCode >> 21) & 4;

            if (Size > 4)
            {
                Instruction = InstDescriptor.Undefined;

                return;
            }

            Extend64 = false;
        }
    }
}