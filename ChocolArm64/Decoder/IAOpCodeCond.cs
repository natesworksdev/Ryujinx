namespace ChocolArm64.Decoder
{
    internal interface IAOpCodeCond : IAOpCode
    {
        ACond Cond { get; }
    }
}