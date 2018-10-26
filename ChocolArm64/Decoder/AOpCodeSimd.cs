using ChocolArm64.Instruction;
using ChocolArm64.State;

namespace ChocolArm64.Decoder
{
    internal class AOpCodeSimd : AOpCode, IAOpCodeSimd
    {
        public int Rd   { get; private   set; }
        public int Rn   { get; private   set; }
        public int Opc  { get; private   set; }
        public int Size { get; protected set; }

        public AOpCodeSimd(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Rd   = (opCode >>  0) & 0x1f;
            Rn   = (opCode >>  5) & 0x1f;
            Opc  = (opCode >> 15) & 0x3;
            Size = (opCode >> 22) & 0x3;

            RegisterSize = ((opCode >> 30) & 1) != 0
                ? ARegisterSize.Simd128
                : ARegisterSize.Simd64;
        }
    }
}