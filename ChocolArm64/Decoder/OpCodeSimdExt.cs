using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class OpCodeSimdExt : OpCodeSimdReg
    {
        public int Imm4 { get; private set; }

        public OpCodeSimdExt(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Imm4 = (opCode >> 11) & 0xf;
        }
    }
}