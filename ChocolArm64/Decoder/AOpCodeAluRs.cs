using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class AOpCodeAluRs : AOpCodeAlu, IAOpCodeAluRs
    {
        public int Shift { get; private set; }
        public int Rm    { get; private set; }

        public AShiftType ShiftType { get; private set; }

        public AOpCodeAluRs(AInst inst, long position, int opCode) : base(inst, position, opCode)
        {
            int shift = (opCode >> 10) & 0x3f;

            if (shift >= GetBitsCount())
            {
                Emitter = AInstEmit.Und;

                return;
            }

            Shift = shift;

            Rm        =              (opCode >> 16) & 0x1f;
            ShiftType = (AShiftType)((opCode >> 22) & 0x3);
        }
    }
}