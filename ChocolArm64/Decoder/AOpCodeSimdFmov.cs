using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class AOpCodeSimdFmov : AOpCode, IAOpCodeSimd
    {
        public int  Rd   { get; private set; }
        public long Imm  { get; private set; }
        public int  Size { get; private set; }

        public AOpCodeSimdFmov(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            int imm5 = (opCode >>  5) & 0x1f;
            int type = (opCode >> 22) & 0x3;

            if (imm5 != 0b00000 || type > 1)
            {
                Emitter = AInstEmit.Und;

                return;
            }

            Size = type;

            long imm;

            Rd  = (opCode >>  0) & 0x1f;
            imm = (opCode >> 13) & 0xff;

            this.Imm = ADecoderHelper.DecodeImm8Float(imm, type);
        }
    }
}