using ChocolArm64.Instructions;

namespace ChocolArm64.Decoders
{
    class OpCodeSimdMemReg : OpCodeMemReg, IOpCodeSimd
    {
        public OpCodeSimdMemReg(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Size |= (opCode >> 21) & 4;

            Extend64 = false;
        }
    }
}