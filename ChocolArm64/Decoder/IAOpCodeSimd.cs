namespace ChocolArm64.Decoder
{
    internal interface IAOpCodeSimd : IAOpCode
    {
        int Size { get; }
    }
}