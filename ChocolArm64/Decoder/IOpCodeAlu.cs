namespace ChocolArm64.Decoder
{
    interface IOpCodeAlu : IOpCode
    {
        int Rd { get; }
        int Rn { get; }

        DataOp DataOp { get; }
    }
}