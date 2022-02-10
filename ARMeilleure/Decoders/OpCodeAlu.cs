namespace ARMeilleure.Decoders
{
    class OpCodeAlu : OpCode, IOpCodeAlu
    {
        public int Rd { get; protected set; }
        public int Rn { get; }

        public DataOp DataOp { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeAlu(inst, address, opCode, inITBlock);

        public OpCodeAlu(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rd     =          (opCode >>  0) & 0x1f;
            Rn     =          (opCode >>  5) & 0x1f;
            DataOp = (DataOp)((opCode >> 24) & 0x3);

            RegisterSize = (opCode >> 31) != 0
                ? RegisterSize.Int64
                : RegisterSize.Int32;
        }
    }
}