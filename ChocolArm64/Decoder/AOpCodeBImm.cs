using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    internal class AOpCodeBImm : AOpCode
    {
        public long Imm { get; protected set; }

        public AOpCodeBImm(AInst inst, long position, int opCode) : base(inst, position, opCode) { }
    }
}