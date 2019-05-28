namespace ARMeilleure.Decoders
{
    class OpCode32MemImm : OpCode32Mem
    {
        public OpCode32MemImm(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            Imm = opCode & 0xfff;
        }
    }
}