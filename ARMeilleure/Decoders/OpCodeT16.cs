namespace ARMeilleure.Decoders
{
    class OpCodeT16 : OpCode32
    {
        public OpCodeT16(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Cond = Condition.Al;

            OpCodeSizeInBytes = 2;
        }
    }
}