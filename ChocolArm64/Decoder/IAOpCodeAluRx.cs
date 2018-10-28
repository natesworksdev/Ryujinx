namespace ChocolArm64.Decoder
{
    interface IaOpCodeAluRx : IaOpCodeAlu
    {
        int Shift { get; }
        int Rm    { get; }

        AIntType IntType { get; }
    }
}