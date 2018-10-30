using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class OpCodeSimdMemPair : OpCodeMemPair, IOpCodeSimd
    {
        public OpCodeSimdMemPair(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Size = ((opCode >> 30) & 3) + 2;

            Extend64 = false;

            DecodeImm(opCode);
        }
    }
}