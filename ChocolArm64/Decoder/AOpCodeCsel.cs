using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class AOpCodeCsel : AOpCodeAlu, IaOpCodeCond
    {
        public int Rm { get; private set; }

        public ACond Cond { get; private set; }

        public AOpCodeCsel(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Rm   =         (opCode >> 16) & 0x1f;
            Cond = (ACond)((opCode >> 12) & 0xf);
        }
    }
}