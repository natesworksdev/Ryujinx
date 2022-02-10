namespace ARMeilleure.Decoders
{
    class OpCode32SimdMovGp : OpCode32, IOpCode32Simd
    {
        public int Size => 2;

        public int Vn { get; }
        public int Rt { get; }
        public int Op { get; }

        public int Opc1 { get; }
        public int Opc2 { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32SimdMovGp(inst, address, opCode, inITBlock);

        public OpCode32SimdMovGp(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            // Which one is used is instruction dependant.
            Op = (opCode >> 20) & 0x1;

            Opc1 = (opCode >> 21) & 0x3;
            Opc2 = (opCode >> 5) & 0x3;

            Vn = ((opCode >> 7) & 0x1) | ((opCode >> 15) & 0x1e);
            Rt = (opCode >> 12) & 0xf;
        }
    }
}
