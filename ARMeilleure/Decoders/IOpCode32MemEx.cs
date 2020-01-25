namespace ARMeilleure.Decoders
{
    interface IOpCode32MemEx : IOpCode32Mem
    {
        public int Rd { get; }
    }
}
