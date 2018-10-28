using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class AOpCodeSimdIns : AOpCodeSimd
    {
        public int SrcIndex { get; private set; }
        public int DstIndex { get; private set; }

        public AOpCodeSimdIns(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            int imm4 = (opCode >> 11) & 0xf;
            int imm5 = (opCode >> 16) & 0x1f;

            if (imm5 == 0b10000)
            {
                Emitter = AInstEmit.Und;

                return;
            }

            Size = imm5 & -imm5;

            switch (Size)
            {
                case 1: Size = 0; break;
                case 2: Size = 1; break;
                case 4: Size = 2; break;
                case 8: Size = 3; break;
            }

            SrcIndex = imm4 >>  Size;
            DstIndex = imm5 >> (Size + 1);
        }
    }
}