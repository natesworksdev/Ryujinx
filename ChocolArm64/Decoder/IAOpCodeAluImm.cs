namespace ChocolArm64.Decoder
{
    internal interface IAOpCodeAluImm : IAOpCodeAlu
    {
        long Imm { get; }
    }
}