using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class AOpCodeSimdMemPair : AOpCodeMemPair, IaOpCodeSimd
    {
        public AOpCodeSimdMemPair(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Size = ((opCode >> 30) & 3) + 2;

            Extend64 = false;

            DecodeImm(opCode);
        }
    }
}