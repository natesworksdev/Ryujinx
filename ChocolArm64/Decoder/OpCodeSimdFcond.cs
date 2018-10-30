using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class OpCodeSimdFcond : OpCodeSimdReg, IOpCodeCond
    {
        public int Nzcv { get; private set; }

        public Cond Cond { get; private set; }

        public OpCodeSimdFcond(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Nzcv =         (opCode >>  0) & 0xf;
            Cond = (Cond)((opCode >> 12) & 0xf);
        }
    }
}