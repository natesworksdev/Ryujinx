using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    internal class AOpCodeAdr : AOpCode
    {
        public int  Rd  { get; private set; }
        public long Imm { get; private set; }

         public AOpCodeAdr(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Rd = opCode & 0x1f;

            Imm  = ADecoderHelper.DecodeImmS19_2(opCode);
            Imm |= ((long)opCode >> 29) & 3;
        }
    }
}