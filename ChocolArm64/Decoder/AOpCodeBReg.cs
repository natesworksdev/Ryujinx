using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    internal class AOpCodeBReg : AOpCode
    {
        public int Rn { get; private set; }

        public AOpCodeBReg(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            int op4 = (opCode >>  0) & 0x1f;
            int op2 = (opCode >> 16) & 0x1f;

            if (op2 != 0b11111 || op4 != 0b00000)
            {
                Emitter = AInstEmit.Und;

                return;
            }

            Rn = (opCode >> 5) & 0x1f;
        }
    }
}