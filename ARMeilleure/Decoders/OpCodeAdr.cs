namespace ARMeilleure.Decoders
{
    class OpCodeAdr : OpCode
    {
        public int Rd { get; }

        public long Immediate { get; }

         public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeAdr(inst, address, opCode, inITBlock);

        public OpCodeAdr(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rd = opCode & 0x1f;

            Immediate  = DecoderHelper.DecodeImmS19_2(opCode);
            Immediate |= ((long)opCode >> 29) & 3;
        }
    }
}