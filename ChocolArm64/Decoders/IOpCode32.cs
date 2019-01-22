namespace ChocolArm64.Decoders
{
    interface IOpCode32 : IOpCode64
    {
        Cond Cond { get; }

        uint GetPc();
    }
}