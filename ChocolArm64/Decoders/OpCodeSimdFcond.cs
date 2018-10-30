using ChocolArm64.Instructions;

namespace ChocolArm64.Decoders
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