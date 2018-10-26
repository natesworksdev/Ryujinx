using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    internal class AOpCodeSimdExt : AOpCodeSimdReg
    {
        public int Imm4 { get; private set; }

        public AOpCodeSimdExt(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Imm4 = (opCode >> 11) & 0xf;
        }
    }
}