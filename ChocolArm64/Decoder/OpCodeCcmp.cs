using ChocolArm64.Instruction;
using ChocolArm64.State;

namespace ChocolArm64.Decoder
{
    class OpCodeCcmp : OpCodeAlu, IOpCodeCond
    {
        public    int Nzcv { get; private set; }
        protected int RmImm;

        public Cond Cond { get; private set; }

        public OpCodeCcmp(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            int o3 = (opCode >> 4) & 1;

            if (o3 != 0)
            {
                Emitter = InstEmit.Und;

                return;
            }

            Nzcv  =         (opCode >>  0) & 0xf;
            Cond  = (Cond)((opCode >> 12) & 0xf);
            RmImm =         (opCode >> 16) & 0x1f;

            Rd = CpuThreadState.ZrIndex;
        }
    }
}