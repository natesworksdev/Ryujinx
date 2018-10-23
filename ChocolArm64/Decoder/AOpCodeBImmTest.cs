using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class AOpCodeBImmTest : AOpCodeBImm
    {
        public int Rt  { get; private set; }
        public int Pos { get; private set; }

        public AOpCodeBImmTest(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Rt = opCode & 0x1f;

            Imm = position + ADecoderHelper.DecodeImmS14_2(opCode);

            Pos  = (opCode >> 19) & 0x1f;
            Pos |= (opCode >> 26) & 0x20;
        }
    }
}