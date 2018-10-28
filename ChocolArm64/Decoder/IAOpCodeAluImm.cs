namespace ChocolArm64.Decoder
{
    interface IaOpCodeAluImm : IaOpCodeAlu
    {
        long Imm { get; }
    }
}