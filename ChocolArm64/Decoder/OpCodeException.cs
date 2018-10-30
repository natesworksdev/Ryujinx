using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class OpCodeException : AOpCode
    {
        public int Id { get; private set; }

        public OpCodeException(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Id = (opCode >> 5) & 0xffff;
        }
    }
}