using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    internal class AOpCodeSimdShImm : AOpCodeSimd
    {
        public int Imm { get; private set; }

        public AOpCodeSimdShImm(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Imm = (opCode >> 16) & 0x7f;

            Size = ABitUtils.HighestBitSetNibble(Imm >> 3);
        }
    }
}
