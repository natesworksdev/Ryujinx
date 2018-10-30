using ChocolArm64.Instructions;

namespace ChocolArm64.Decoders
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