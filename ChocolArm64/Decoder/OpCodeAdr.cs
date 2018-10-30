using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class OpCodeAdr : AOpCode
    {
        public int  Rd  { get; private set; }
        public long Imm { get; private set; }

         public OpCodeAdr(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Rd = opCode & 0x1f;

            Imm  = DecoderHelper.DecodeImmS19_2(opCode);
            Imm |= ((long)opCode >> 29) & 3;
        }
    }
}