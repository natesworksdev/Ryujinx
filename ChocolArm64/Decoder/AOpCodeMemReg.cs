using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    internal class AOpCodeMemReg : AOpCodeMem
    {
        public bool Shift { get; private set; }
        public int  Rm    { get; private set; }

        public AIntType IntType { get; private set; }

        public AOpCodeMemReg(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Shift    =           ((opCode >> 12) & 0x1) != 0;
            IntType  = (AIntType)((opCode >> 13) & 0x7);
            Rm       =            (opCode >> 16) & 0x1f;
            Extend64 =           ((opCode >> 22) & 0x3) == 2;
        }
    }
}