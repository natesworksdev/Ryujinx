namespace ChocolArm64.Decoder
{
    interface IOpCodeCond : IOpCode
    {
        Cond Cond { get; }
    }
}