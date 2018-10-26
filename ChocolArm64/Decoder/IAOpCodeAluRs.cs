namespace ChocolArm64.Decoder
{
    internal interface IAOpCodeAluRs : IAOpCodeAlu
    {
        int Shift { get; }
        int Rm    { get; }

        AShiftType ShiftType { get; }
    }
}