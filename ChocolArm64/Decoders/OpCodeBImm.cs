using ChocolArm64.Instructions;

namespace ChocolArm64.Decoders
{
    class OpCodeBImm : AOpCode
    {
        public long Imm { get; protected set; }

        public OpCodeBImm(Inst inst, long position, int opCode) : base(inst, position, opCode) { }
    }
}