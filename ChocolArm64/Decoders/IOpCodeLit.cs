namespace ChocolArm64.Decoders
{
    interface IOpCodeLit : IOpCode
    {
        int  Rt       { get; }
        long Imm      { get; }
        int  Size     { get; }
        bool Signed   { get; }
        bool Prefetch { get; }
    }
}