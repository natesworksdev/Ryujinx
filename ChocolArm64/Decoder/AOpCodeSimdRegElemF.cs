using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    internal class AOpCodeSimdRegElemF : AOpCodeSimdReg
    {
        public int Index { get; private set; }

        public AOpCodeSimdRegElemF(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            switch ((opCode >> 21) & 3) // sz:L
            {
                case 0: // H:0
                    Index = (opCode >> 10) & 2; // 0, 2

                    break;

                case 1: // H:1
                    Index = (opCode >> 10) & 2;
                    Index++; // 1, 3

                    break;

                case 2: // H
                    Index = (opCode >> 11) & 1; // 0, 1

                    break;

                default: Emitter = AInstEmit.Und; return;
            }
        }
    }
}
