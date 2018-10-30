using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class OpCodeSimdReg : OpCodeSimd
    {
        public bool Bit3 { get; private   set; }
        public int  Ra   { get; private   set; }
        public int  Rm   { get; protected set; }

        public OpCodeSimdReg(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Bit3 = ((opCode >>  3) & 0x1) != 0;
            Ra   =  (opCode >> 10) & 0x1f;
            Rm   =  (opCode >> 16) & 0x1f;
        }
    }
}