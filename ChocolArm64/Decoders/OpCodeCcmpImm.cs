using ChocolArm64.Instructions;

namespace ChocolArm64.Decoders
{
    class OpCodeCcmpImm : OpCodeCcmp, IOpCodeAluImm
    {
        public long Imm => RmImm;

        public OpCodeCcmpImm(Inst inst, long position, int opCode) : base(inst, position, opCode) { }
    }
}