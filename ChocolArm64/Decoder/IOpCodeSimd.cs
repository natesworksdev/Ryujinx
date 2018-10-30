namespace ChocolArm64.Decoder
{
    interface IOpCodeSimd : IOpCode
    {
        int Size { get; }
    }
}