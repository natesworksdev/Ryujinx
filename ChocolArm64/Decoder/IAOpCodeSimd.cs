namespace ChocolArm64.Decoder
{
    interface IaOpCodeSimd : IaOpCode
    {
        int Size { get; }
    }
}