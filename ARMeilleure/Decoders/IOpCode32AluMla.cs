namespace ARMeilleure.Decoders
{
    interface IOpCode32AluMla : IOpCode32AluReg
    {
        public int Ra { get; }

        public bool NHigh { get; }
        public bool MHigh { get; }
        public bool R { get; }
    }
}
