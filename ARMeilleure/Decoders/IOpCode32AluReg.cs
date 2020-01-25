namespace ARMeilleure.Decoders
{
    interface IOpCode32AluReg : IOpCode32Alu
    {
        public int Rm { get; }
    }
}
