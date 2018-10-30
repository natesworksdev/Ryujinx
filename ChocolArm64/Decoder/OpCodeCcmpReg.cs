using ChocolArm64.Instruction;

namespace ChocolArm64.Decoder
{
    class OpCodeCcmpReg : OpCodeCcmp, IOpCodeAluRs
    {
        public int Rm => RmImm;

        public int Shift => 0;

        public ShiftType ShiftType => ShiftType.Lsl;

        public OpCodeCcmpReg(Inst inst, long position, int opCode) : base(inst, position, opCode) { }
    }
}