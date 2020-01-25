namespace ARMeilleure.Decoders
{
    interface IOpCode32SimdImm : IOpCode32Simd
    {
        public int Vd { get; }
        public long Immediate { get; }
        int Elems { get; }
    }
}
