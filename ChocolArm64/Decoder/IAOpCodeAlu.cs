namespace ChocolArm64.Decoder
{
    internal interface IAOpCodeAlu : IAOpCode
    {
        int Rd { get; }
        int Rn { get; }

        ADataOp DataOp { get; }
    }
}