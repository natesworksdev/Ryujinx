using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    internal class AOpCodeBImmAl : AOpCodeBImm
    {
        public AOpCodeBImmAl(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Imm = position + ADecoderHelper.DecodeImm26_2(opCode);
        }
    }
}