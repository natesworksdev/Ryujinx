using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class OpCodeCcmpImm : OpCodeCcmp, IOpCodeAluImm
    {
        public long Imm => RmImm;

        public OpCodeCcmpImm(Inst inst, long position, int opCode) : base(inst, position, opCode) { }
    }
}