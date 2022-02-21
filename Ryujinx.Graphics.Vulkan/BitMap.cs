namespace Ryujinx.Graphics.Vulkan
{
    struct BitMap
    {
        private const int IntSize = 64;
        private const int IntMask = IntSize - 1;

        private readonly long[] _masks;

        public BitMap(int count)
        {
            _masks = new long[(count + IntMask) / IntSize];
        }

        public bool AnySet()
        {
            for (int i = 0; i < _masks.Length; i++)
            {
                if (_masks[i] != 0)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsSet(int bit)
        {
            int wordIndex = bit / IntSize;
            int wordBit = bit & IntMask;

            long wordMask = 1L << wordBit;

            return (_masks[wordIndex] & wordMask) != 0;
        }

        public bool Set(int bit)
        {
            int wordIndex = bit / IntSize;
            int wordBit   = bit & IntMask;

            long wordMask = 1L << wordBit;

            if ((_masks[wordIndex] & wordMask) != 0)
            {
                return false;
            }

            _masks[wordIndex] |= wordMask;

            return true;
        }

        public void Clear(int bit)
        {
            int wordIndex = bit / IntSize;
            int wordBit   = bit & IntMask;

            long wordMask = 1L << wordBit;

            _masks[wordIndex] &= ~wordMask;
        }
    }
}