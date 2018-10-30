using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class OpCodeMemEx : OpCodeMem
    {
        public int Rt2 { get; private set; }
        public int Rs  { get; private set; }

        public OpCodeMemEx(Inst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Rt2 = (opCode >> 10) & 0x1f;
            Rs  = (opCode >> 16) & 0x1f;
        }
    }
}