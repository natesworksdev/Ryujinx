using ChocolArm64.Instruction;
using ChocolArm64.State;

namespace ChocolArm64.Decoder
{
    class AOpCodeCcmp : AOpCodeAlu, IaOpCodeCond
    {
        public    int Nzcv { get; private set; }
        protected int RmImm;

        public ACond Cond { get; private set; }

        public AOpCodeCcmp(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            int o3 = (opCode >> 4) & 1;

            if (o3 != 0)
            {
                Emitter = AInstEmit.Und;

                return;
            }

            Nzcv  =         (opCode >>  0) & 0xf;
            Cond  = (ACond)((opCode >> 12) & 0xf);
            RmImm =         (opCode >> 16) & 0x1f;

            Rd = AThreadState.ZrIndex;
        }
    }
}