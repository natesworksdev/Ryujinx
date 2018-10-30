using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder32
{
    class A32OpCodeBImmAl : A32OpCode
    {
        public int Imm;
        public int H;

        public A32OpCodeBImmAl(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Imm = (opCode <<  8) >> 6;
            H   = (opCode >> 23) &  2;
        }
    }
}