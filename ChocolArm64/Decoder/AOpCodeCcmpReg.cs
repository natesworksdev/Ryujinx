using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class AOpCodeCcmpReg : AOpCodeCcmp, IaOpCodeAluRs
    {
        public int Rm => RmImm;

        public int Shift => 0;

        public AShiftType ShiftType => AShiftType.Lsl;

        public AOpCodeCcmpReg(AInst inst, long position, int opCode) : base(inst, position, opCode) { }
    }
}