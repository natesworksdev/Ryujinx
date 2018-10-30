namespace ChocolArm64.Decoder
{
    interface IOpCodeAluRx : IOpCodeAlu
    {
        int Shift { get; }
        int Rm    { get; }

        IntType IntType { get; }
    }
}