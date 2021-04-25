namespace ARMeilleure.Decoders
{
    class OpCode32SimdMovNarrow : OpCode32Simd
    {
        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode) => new OpCode32SimdMovNarrow(inst, address, opCode);

        public OpCode32SimdMovNarrow(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Size = (opCode >> 18) & 0x3;
            Opc = (opCode >> 6) & 0x3;

            if (Size == 3 && ((Vm & 1) == 1))
            {
                Instruction = InstDescriptor.Undefined;
            }
        }
    }
}
