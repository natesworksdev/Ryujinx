using ChocolArm64.Instructions;

namespace ChocolArm64.Decoders
{
    class OpCodeSimdTbl : OpCodeSimdReg
    {
        public OpCodeSimdTbl(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Size = ((opCode >> 13) & 3) + 1;
        }
    }
}