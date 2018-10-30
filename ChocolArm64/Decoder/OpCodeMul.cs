using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class OpCodeMul : OpCodeAlu
    {
        public int Rm { get; private set; }
        public int Ra { get; private set; }

        public OpCodeMul(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Ra = (opCode >> 10) & 0x1f;
            Rm = (opCode >> 16) & 0x1f;
        }
    }
}