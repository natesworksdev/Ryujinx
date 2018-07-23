using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder32
{
    class A32OpCodeBReg : A32OpCode
    {
        public int Rm { get; private set; }

        public A32OpCodeBReg(AInst Inst, long Position, int OpCode) : base(Inst, Position, OpCode)
        {
            Rm = (OpCode >> 0) & 0xf;
        }
    }
}