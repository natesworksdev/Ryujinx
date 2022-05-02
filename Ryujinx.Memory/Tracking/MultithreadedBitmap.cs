using System;
using System.Threading;

namespace Ryujinx.Memory.Tracking
{
    internal class MultithreadedBitmap
    {
        public const int IntSize = 64;

        public const int IntShift = 6;
        public const int IntMask = IntSize - 1;

        public readonly long[] Masks;

        public MultithreadedBitmap(int count, bool set)
        {
            Masks = new long[(count + IntMask) / IntSize];

            if (set)
            {
                Array.Fill(Masks, -1L);
            }
        }

        public bool AnySet()
        {
            for (int i = 0; i < Masks.Length; i++)
            {
                if (Volatile.Read(ref Masks[i]) != 0)
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

            return (Volatile.Read(ref Masks[wordIndex]) & wordMask) != 0;
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

            long startValue = Volatile.Read(ref Masks[startIndex]);

            if (startIndex == endIndex)
            {
                return (startValue & startMask & endMask) != 0;
            }

            if ((startValue & startMask) != 0)
            {
                return true;
            }

            for (int i = startIndex + 1; i < endIndex; i++)
            {
                if (Volatile.Read(ref Masks[i]) != 0)
                {
                    return true;
                }
            }

            long endValue = Volatile.Read(ref Masks[endIndex]);

            if ((endValue & endMask) != 0)
            {
                return true;
            }

            return false;
        }

        public void Set(int bit, bool value)
        {
            int wordIndex = bit >> IntShift;
            int wordBit = bit & IntMask;

            long wordMask = 1L << wordBit;

            long existing;
            long newValue;

            do
            {
                existing = Volatile.Read(ref Masks[wordIndex]);
                
                if (value)
                {
                    newValue = existing | wordMask;
                }
                else
                {
                    newValue = existing & ~wordMask;
                }
            }
            while (Interlocked.CompareExchange(ref Masks[wordIndex], newValue, existing) != existing);
        }

        /*
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
                _masks[startIndex] |= startMask & endMask;
            }
            else
            {
                _masks[startIndex] |= startMask;

                for (int i = startIndex + 1; i < endIndex; i++)
                {
                    _masks[i] |= -1;
                }

                _masks[endIndex] |= endMask;
            }
        }
        */

        /*
        public void Clear(int bit)
        {
            int wordIndex = bit >> IntShift;
            int wordBit = bit & IntMask;

            long wordMask = 1L << wordBit;

            _masks[wordIndex] &= ~wordMask;
        }
        */

        public void Clear()
        {
            for (int i = 0; i < Masks.Length; i++)
            {
                Volatile.Write(ref Masks[i], 0);
            }
        }

        public void ClearInt(int start, int end)
        {
            for (int i = start; i <= end; i++)
            {
                Volatile.Write(ref Masks[i], 0);
            }
        }
    }
}
