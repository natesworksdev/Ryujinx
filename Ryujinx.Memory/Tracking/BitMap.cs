namespace Ryujinx.Memory.Tracking
{
    struct BitMap
    {
        public const int IntSize = 64;

        private const int IntShift = 6;
        private const int IntMask = IntSize - 1;

        public readonly long[] Masks;

        public BitMap(int count)
        {
            Masks = new long[(count + IntMask) / IntSize];
        }

        public bool AnySet()
        {
            for (int i = 0; i < Masks.Length; i++)
            {
                if (Masks[i] != 0)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsSet(int bit)
        {
            int wordIndex = bit >> IntShift;
            int wordBit = bit & IntMask;

            long wordMask = 1L << wordBit;

            return (Masks[wordIndex] & wordMask) != 0;
        }

        public bool IsSet(int start, int end)
        {
            if (start == end)
            {
                return IsSet(start);
            }

            int startIndex = start >> IntShift;
            int startBit = start & IntMask;
            long startMask = -1L << startBit;

            int endIndex = end >> IntShift;
            int endBit = end & IntMask;
            long endMask = (long)(ulong.MaxValue >> (IntMask - endBit));

            if (startIndex == endIndex)
            {
                return (Masks[startIndex] & startMask & endMask) != 0;
            }

            if ((Masks[startIndex] & startMask) != 0)
            {
                return true;
            }

            for (int i = startIndex + 1; i < endIndex; i++)
            {
                if (Masks[i] != 0)
                {
                    return true;
                }
            }

            if ((Masks[endIndex] & endMask) != 0)
            {
                return true;
            }

            return false;
        }

        public bool Set(int bit)
        {
            int wordIndex = bit >> IntShift;
            int wordBit = bit & IntMask;

            long wordMask = 1L << wordBit;

            if ((Masks[wordIndex] & wordMask) != 0)
            {
                return false;
            }

            Masks[wordIndex] |= wordMask;

            return true;
        }

        public void SetRange(int start, int end)
        {
            if (start == end)
            {
                Set(start);
                return;
            }

            int startIndex = start >> IntShift;
            int startBit = start & IntMask;
            long startMask = -1L << startBit;

            int endIndex = end >> IntShift;
            int endBit = end & IntMask;
            long endMask = (long)(ulong.MaxValue >> (IntMask - endBit));

            if (startIndex == endIndex)
            {
                Masks[startIndex] |= startMask & endMask;
            }
            else
            {
                Masks[startIndex] |= startMask;

                for (int i = startIndex + 1; i < endIndex; i++)
                {
                    Masks[i] |= -1;
                }

                Masks[endIndex] |= endMask;
            }
        }

        public void Clear(int bit)
        {
            int wordIndex = bit >> IntShift;
            int wordBit = bit & IntMask;

            long wordMask = 1L << wordBit;

            Masks[wordIndex] &= ~wordMask;
        }

        public void Clear()
        {
            for (int i = 0; i < Masks.Length; i++)
            {
                Masks[i] = 0;
            }
        }

        public void ClearInt(int start, int end)
        {
            for (int i = start; i <= end; i++)
            {
                Masks[i] = 0;
            }
        }
    }
}
