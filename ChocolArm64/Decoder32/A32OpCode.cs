using ChocolArm64.Decoder;
using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder32
{
    class A32OpCode : AOpCode
    {
        public Cond Cond { get; private set; }

        public A32OpCode(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Cond = (Cond)((uint)opCode >> 28);
        }
    }
}