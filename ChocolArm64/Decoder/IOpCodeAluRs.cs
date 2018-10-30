namespace ChocolArm64.Decoder
{
    interface IOpCodeAluRs : IOpCodeAlu
    {
        int Shift { get; }
        int Rm    { get; }

        ShiftType ShiftType { get; }
    }
}