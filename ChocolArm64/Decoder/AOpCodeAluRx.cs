using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    internal class AOpCodeAluRx : AOpCodeAlu, IAOpCodeAluRx
    {
        public int Shift { get; private set; }
        public int Rm    { get; private set; }

        public AIntType IntType { get; private set; }

        public AOpCodeAluRx(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            Shift   =            (opCode >> 10) & 0x7;
            IntType = (AIntType)((opCode >> 13) & 0x7);
            Rm      =            (opCode >> 16) & 0x1f;
        }
    }
}