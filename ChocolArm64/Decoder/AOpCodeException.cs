using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class AOpCodeException : AOpCode
    {
        public int Id { get; private set; }

        public AOpCodeException(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Id = (opCode >> 5) & 0xffff;
        }
    }
}