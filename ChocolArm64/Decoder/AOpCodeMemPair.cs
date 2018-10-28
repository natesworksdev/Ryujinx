using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class AOpCodeMemPair : AOpCodeMemImm
    {
        public int Rt2 { get; private set; }

        public AOpCodeMemPair(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Rt2      =  (opCode >> 10) & 0x1f;
            WBack    = ((opCode >> 23) & 0x1) != 0;
            PostIdx  = ((opCode >> 23) & 0x3) == 1;
            Extend64 = ((opCode >> 30) & 0x3) == 1;
            Size     = ((opCode >> 31) & 0x1) | 2;

            DecodeImm(opCode);
        }

        protected void DecodeImm(int opCode)
        {
            Imm = ((long)(opCode >> 15) << 57) >> (57 - Size);
        }
    }
}