namespace ChocolArm64.Decoders
{
    interface IOpCodeCond : IOpCode
    {
        Cond Cond { get; }
    }
}