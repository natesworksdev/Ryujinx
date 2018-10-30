using ChocolArm64.Instructions;

namespace ChocolArm64.Decoders
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