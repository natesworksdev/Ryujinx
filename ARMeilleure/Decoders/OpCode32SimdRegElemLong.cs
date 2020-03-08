namespace ARMeilleure.Decoders
{
    class OpCode32SimdRegElemLong : OpCode32SimdReg
    {
        public OpCode32SimdRegElemLong(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Q = false;
            Size = (opCode >> 20) & 0x3;

            RegisterSize = RegisterSize.Simd64;

            if (Size == 1)
            {
                Vm = ((opCode >> 3) & 0x1) | ((opCode >> 4) & 0x2) | ((opCode << 2) & 0x1c);
            }
            else /* if (Size == 2) */
            {
                Vm = ((opCode >> 5) & 0x1) | ((opCode << 1) & 0x1e);
            }

            // (Vd & 1) != 0 || Size == 3 are also invalid, but they are checked on encoding.
            if (Size == 0)
            {
                Instruction = InstDescriptor.Undefined;
            }
        }
    }
}
