namespace ARMeilleure.Decoders
{
    class OpCode32SimdRegMultLong : OpCode32SimdReg
    {

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode) => new OpCode32SimdRegMultLong(inst, address, opCode);

        public OpCode32SimdRegMultLong(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Opc = (opCode >> 9) & 0x1;

            // Subclasses have their own handling of Vx to account for before checking.
            if (GetType() == typeof(OpCode32SimdRegMultLong) && (Vd & 1) == 1)
            {
                Instruction = InstDescriptor.Undefined;
            }
        }
    }
}
