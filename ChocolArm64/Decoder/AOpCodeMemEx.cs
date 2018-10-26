using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    internal class AOpCodeMemEx : AOpCodeMem
    {
        public int Rt2 { get; private set; }
        public int Rs  { get; private set; }

        public AOpCodeMemEx(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Rt2 = (opCode >> 10) & 0x1f;
            Rs  = (opCode >> 16) & 0x1f;
        }
    }
}