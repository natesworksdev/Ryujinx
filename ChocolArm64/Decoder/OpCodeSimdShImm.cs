using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class OpCodeSimdShImm : OpCodeSimd
    {
        public int Imm { get; private set; }

        public OpCodeSimdShImm(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Imm = (opCode >> 16) & 0x7f;

            Size = BitUtils.HighestBitSetNibble(Imm >> 3);
        }
    }
}
