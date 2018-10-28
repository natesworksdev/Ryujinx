namespace ChocolArm64.Decoder
{
    interface IaOpCodeCond : IaOpCode
    {
        ACond Cond { get; }
    }
}