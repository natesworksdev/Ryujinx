using ARMeilleure.Common;

namespace ARMeilleure.Decoders
{
    class OpCodeSimdShImm : OpCodeSimd
    {
        public int Imm { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeSimdShImm(inst, address, opCode, inITBlock);

        public OpCodeSimdShImm(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            Imm = (opCode >> 16) & 0x7f;

            Size = BitUtils.HighestBitSetNibble(Imm >> 3);
        }
    }
}
