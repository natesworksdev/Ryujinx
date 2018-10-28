using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class AOpCodeMul : AOpCodeAlu
    {
        public int Rm { get; private set; }
        public int Ra { get; private set; }

        public AOpCodeMul(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Ra = (opCode >> 10) & 0x1f;
            Rm = (opCode >> 16) & 0x1f;
        }
    }
}