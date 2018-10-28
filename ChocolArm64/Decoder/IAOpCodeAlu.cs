namespace ChocolArm64.Decoder
{
    interface IaOpCodeAlu : IaOpCode
    {
        int Rd { get; }
        int Rn { get; }

        ADataOp DataOp { get; }
    }
}