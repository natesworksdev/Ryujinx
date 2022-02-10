namespace ARMeilleure.Decoders
{
    class OpCode32MemLdEx : OpCode32Mem, IOpCode32MemEx
    {
        public int Rd { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCode32MemLdEx(inst, address, opCode, inITBlock);

        public OpCode32MemLdEx(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Rd = opCode & 0xf;
        }
    }
}
