using ChocolArm64.Decoder;
using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder32
{
    internal class A32OpCode : AOpCode
    {
        public ACond Cond { get; private set; }

        public A32OpCode(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Cond = (ACond)((uint)opCode >> 28);
        }
    }
}