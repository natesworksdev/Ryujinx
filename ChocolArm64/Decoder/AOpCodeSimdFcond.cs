using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    internal class AOpCodeSimdFcond : AOpCodeSimdReg, IAOpCodeCond
    {
        public int Nzcv { get; private set; }

        public ACond Cond { get; private set; }

        public AOpCodeSimdFcond(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Nzcv =         (opCode >>  0) & 0xf;
            Cond = (ACond)((opCode >> 12) & 0xf);
        }
    }
}