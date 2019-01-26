namespace ChocolArm64.Decoders
{
    interface IOpCodeMemMult32 : IOpCode32
    {
        int Rn { get; }

        int RegisterMask { get; }

        int PostOffset { get; }
    }
}