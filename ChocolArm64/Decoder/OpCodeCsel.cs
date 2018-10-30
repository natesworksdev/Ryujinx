using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class OpCodeCsel : OpCodeAlu, IOpCodeCond
    {
        public int Rm { get; private set; }

        public Cond Cond { get; private set; }

        public OpCodeCsel(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Rm   =         (opCode >> 16) & 0x1f;
            Cond = (Cond)((opCode >> 12) & 0xf);
        }
    }
}