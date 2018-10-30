namespace ChocolArm64.Decoders
{
    interface IOpCodeAluImm : IOpCodeAlu
    {
        long Imm { get; }
    }
}