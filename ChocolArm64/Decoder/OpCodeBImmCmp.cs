using ChocolArm64.Instruction;
using ChocolArm64.State;

namespace ChocolArm64.Decoder
{
    class OpCodeBImmCmp : OpCodeBImm
    {
        public int Rt { get; private set; }

        public OpCodeBImmCmp(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Rt = opCode & 0x1f;

            Imm = position + DecoderHelper.DecodeImmS19_2(opCode);

            RegisterSize = (opCode >> 31) != 0
                ? State.RegisterSize.Int64
                : State.RegisterSize.Int32;
        }
    }
}