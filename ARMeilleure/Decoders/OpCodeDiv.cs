namespace ARMeilleure.Decoders
{
    class OpCodeDiv : OpCodeAlu
    {
        public int Rm { get; private set; }

        public OpCodeDiv(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Rm = (opCode >> 16) & 0x1f;
        }
    }
}