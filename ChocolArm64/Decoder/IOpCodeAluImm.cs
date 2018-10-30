namespace ChocolArm64.Decoder
{
    interface IOpCodeAluImm : IOpCodeAlu
    {
        long Imm { get; }
    }
}