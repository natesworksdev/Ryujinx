namespace ChocolArm64.Decoders
{
    interface IOpCodeMem32 : IOpCode32
    {
        int Rt { get; }
        int Rn { get; }

        bool WBack { get; }
    }
}