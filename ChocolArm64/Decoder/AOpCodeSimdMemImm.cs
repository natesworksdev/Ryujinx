using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class AOpCodeSimdMemImm : AOpCodeMemImm, IAOpCodeSimd
    {
        public AOpCodeSimdMemImm(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Size |= (opCode >> 21) & 4;

            if (!WBack && !Unscaled && Size >= 4)
            {
                Imm <<= 4;
            }

            Extend64 = false;
        }
    }
}