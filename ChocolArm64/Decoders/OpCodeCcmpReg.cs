using ChocolArm64.Instructions;

namespace ChocolArm64.Decoders
{
    class OpCodeCcmpReg : OpCodeCcmp, IOpCodeAluRs
    {
        public int Rm => RmImm;

        public int Shift => 0;

        public ShiftType ShiftType => ShiftType.Lsl;

        public OpCodeCcmpReg(Inst inst, long position, int opCode) : base(inst, position, opCode) { }
    }
}