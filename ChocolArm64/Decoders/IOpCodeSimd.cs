namespace ChocolArm64.Decoders
{
    interface IOpCodeSimd : IOpCode
    {
        int Size { get; }
    }
}