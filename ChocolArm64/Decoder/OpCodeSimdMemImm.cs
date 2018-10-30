using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class OpCodeSimdMemImm : OpCodeMemImm, IOpCodeSimd
    {
        public OpCodeSimdMemImm(Inst inst, long position, int opCode) : base(inst, position, opCode)
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