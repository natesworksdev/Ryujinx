using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class OpCodeBImmCond : OpCodeBImm, IOpCodeCond
    {
        public Cond Cond { get; private set; }

        public OpCodeBImmCond(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            int o0 = (opCode >> 4) & 1;

            if (o0 != 0)
            {
                Emitter = InstEmit.Und;

                return;
            }

            Cond = (Cond)(opCode & 0xf);

            Imm = position + DecoderHelper.DecodeImmS19_2(opCode);
        }
    }
}