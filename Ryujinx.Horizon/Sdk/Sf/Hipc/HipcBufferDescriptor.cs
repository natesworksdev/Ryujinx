namespace Ryujinx.Horizon.Sdk.Sf.Hipc
{
    struct HipcBufferDescriptor
    {
        private uint _sizeLow;
        private uint _addressLow;
        private uint _word2;

        public ulong Address => _addressLow | (((ulong)_word2 << 4) & 0xf00000000UL) | (((ulong)_word2 << 34) & 0x7000000000UL);
        public ulong Size => _sizeLow | ((ulong)_word2 << 8) & 0xf00000000UL;
        public HipcBufferMode Mode => (HipcBufferMode)(_word2 & 3);
    }
}
