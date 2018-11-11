using ChocolArm64.Instructions;

namespace ChocolArm64.Decoders32
{
    class A32OpCodeBReg : A32OpCode
    {
        public int Rm { get; private set; }

        public A32OpCodeBReg(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Rm = (opCode >> 0) & 0xf;
        }
    }
}