using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class OpCodeSimdTbl : OpCodeSimdReg
    {
        public OpCodeSimdTbl(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Size = ((opCode >> 13) & 3) + 1;
        }
    }
}