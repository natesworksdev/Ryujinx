namespace ChocolArm64.Decoder
{
    interface IaOpCodeAluRs : IaOpCodeAlu
    {
        int Shift { get; }
        int Rm    { get; }

        AShiftType ShiftType { get; }
    }
}