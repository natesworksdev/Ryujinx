using ChocolArm64.Instruction;
using ChocolArm64.State;

namespace ChocolArm64.Decoder
{
    class AOpCodeMov : AOpCode
    {
        public int  Rd  { get; private set; }
        public long Imm { get; private set; }
        public int  Pos { get; private set; }

        public AOpCodeMov(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            int p1 = (opCode >> 22) & 1;
            int sf = (opCode >> 31) & 1;

            if (sf == 0 && p1 != 0)
            {
                Emitter = AInstEmit.Und;

                return;
            }

            Rd  = (opCode >>  0) & 0x1f;
            Imm = (opCode >>  5) & 0xffff;
            Pos = (opCode >> 21) & 0x3;

            Pos <<= 4;
            Imm <<= Pos;

            RegisterSize = (opCode >> 31) != 0
                ? ARegisterSize.Int64
                : ARegisterSize.Int32;
        }
    }
}