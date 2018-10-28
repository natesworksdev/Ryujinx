using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class AOpCodeSimdRegElem : AOpCodeSimdReg
    {
        public int Index { get; private set; }

        public AOpCodeSimdRegElem(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            switch (Size)
            {
                case 1:
                    Index = (opCode >> 20) & 3 |
                            (opCode >>  9) & 4;

                    Rm &= 0xf;

                    break;

                case 2:
                    Index = (opCode >> 21) & 1 |
                            (opCode >> 10) & 2;

                    break;

                default: Emitter = AInstEmit.Und; return;
            }
        }
    }
}