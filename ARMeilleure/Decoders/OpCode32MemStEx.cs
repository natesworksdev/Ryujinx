namespace ARMeilleure.Decoders
{
    class OpCode32MemStEx : OpCode32Mem, IOpCode32MemEx
    {
        public int Rd { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32MemStEx(inst, address, opCode, inITBlock);

        public OpCode32MemStEx(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rd = (opCode >> 12) & 0xf;
            Rt = (opCode >> 0) & 0xf;
        }
    }
}
