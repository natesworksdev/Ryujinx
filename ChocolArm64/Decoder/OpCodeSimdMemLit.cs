using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class OpCodeSimdMemLit : AOpCode, IOpCodeSimd, IOpCodeLit
    {
        public int  Rt   { get; private set; }
        public long Imm  { get; private set; }
        public int  Size { get; private set; }
        public bool Signed   => false;
        public bool Prefetch => false;

        public OpCodeSimdMemLit(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            int opc = (opCode >> 30) & 3;

            if (opc == 3)
            {
                Emitter = InstEmit.Und;

                return;
            }

            Rt = opCode & 0x1f;

            Imm = position + DecoderHelper.DecodeImmS19_2(opCode);

            Size = opc + 2;
        }
    }
}