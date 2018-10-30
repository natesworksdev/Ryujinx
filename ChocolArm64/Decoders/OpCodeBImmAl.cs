using ChocolArm64.Instructions;

namespace ChocolArm64.Decoders
{
    class OpCodeBImmAl : OpCodeBImm
    {
        public OpCodeBImmAl(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Imm = position + DecoderHelper.DecodeImm26_2(opCode);
        }
    }
}