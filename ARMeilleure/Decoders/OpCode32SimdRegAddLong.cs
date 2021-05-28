namespace ARMeilleure.Decoders
{
    class OpCode32SimdRegAddLong : OpCode32SimdReg
    {

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode) => new OpCode32SimdRegAddLong(inst, address, opCode);

        public OpCode32SimdRegAddLong(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Size = (opCode >> 18) & 0x3;
            Opc = (opCode >> 7) & 0x1;

            // Subclasses have their own handling of Vx to account for before checking.
            if (GetType() == typeof(OpCode32SimdRegAddLong) && DecoderHelper.VectorArgumentsInvalid(Q, Vd, Vm))
            {
                Instruction = InstDescriptor.Undefined;
            }
        }
    }
}
