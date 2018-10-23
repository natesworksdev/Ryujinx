using ChocolArm64.Instruction;
using ChocolArm64.State;

namespace ChocolArm64.Decoder
{
    class AOpCodeSimdCvt : AOpCodeSimd
    {
        public int FBits { get; private set; }

        public AOpCodeSimdCvt(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            //TODO:
            //Und of Fixed Point variants.
            int scale = (opCode >> 10) & 0x3f;
            int sf    = (opCode >> 31) & 0x1;

            /*if (Type != SF && !(Type == 2 && SF == 1))
            {
                Emitter = AInstEmit.Und;

                return;
            }*/

            FBits = 64 - scale;

            RegisterSize = sf != 0
                ? ARegisterSize.Int64
                : ARegisterSize.Int32;
        }
    }
}