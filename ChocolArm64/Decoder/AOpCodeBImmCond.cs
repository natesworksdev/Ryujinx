using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class AOpCodeBImmCond : AOpCodeBImm, IAOpCodeCond
    {
        public ACond Cond { get; private set; }

        public AOpCodeBImmCond(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            int o0 = (opCode >> 4) & 1;

            if (o0 != 0)
            {
                Emitter = AInstEmit.Und;

                return;
            }

            Cond = (ACond)(opCode & 0xf);

            Imm = position + ADecoderHelper.DecodeImmS19_2(opCode);
        }
    }
}