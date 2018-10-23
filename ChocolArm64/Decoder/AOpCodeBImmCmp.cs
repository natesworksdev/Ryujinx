using ChocolArm64.Instruction;
using ChocolArm64.State;

namespace ChocolArm64.Decoder
{
    class AOpCodeBImmCmp : AOpCodeBImm
    {
        public int Rt { get; private set; }

        public AOpCodeBImmCmp(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Rt = opCode & 0x1f;

            Imm = position + ADecoderHelper.DecodeImmS19_2(opCode);

            RegisterSize = (opCode >> 31) != 0
                ? ARegisterSize.Int64
                : ARegisterSize.Int32;
        }
    }
}