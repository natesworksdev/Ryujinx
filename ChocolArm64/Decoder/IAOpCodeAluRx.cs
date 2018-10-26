namespace ChocolArm64.Decoder
{
    internal interface IAOpCodeAluRx : IAOpCodeAlu
    {
        int Shift { get; }
        int Rm    { get; }

        AIntType IntType { get; }
    }
}