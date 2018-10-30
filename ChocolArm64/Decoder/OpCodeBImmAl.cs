using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class OpCodeBImmAl : OpCodeBImm
    {
        public OpCodeBImmAl(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Imm = position + DecoderHelper.DecodeImm26_2(opCode);
        }
    }
}