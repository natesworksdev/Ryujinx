using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class OpCodeBImm : AOpCode
    {
        public long Imm { get; protected set; }

        public OpCodeBImm(Inst inst, long position, int opCode) : base(inst, position, opCode) { }
    }
}