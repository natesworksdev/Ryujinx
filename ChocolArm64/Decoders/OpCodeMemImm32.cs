using ChocolArm64.Instructions;

namespace ChocolArm64.Decoders
{
    class OpCodeMemImm32 : OpCodeMem32
    {
        public OpCodeMemImm32(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Imm = opCode & 0xfff;
        }
    }
}