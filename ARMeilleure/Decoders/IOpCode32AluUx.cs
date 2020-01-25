namespace ARMeilleure.Decoders
{
    interface IOpCode32AluUx : IOpCode32AluReg
    {
        public int RotateBits { get; }
        public bool Add { get; }
    }
}
