using System;

namespace Ryujinx.Audio
{
    class AudioHelper
    {
        public byte GetHighNibble(byte Value)
        {
            return (byte)((Value >> 4) & 0xF);
        }

        public byte GetLowNibble(byte Value)
        {
            return (byte)(Value & 0xF);
        }

        public short Clamp16(int Value)
        {
            if (Value > short.MaxValue)
                return short.MaxValue;
            if (Value < short.MinValue)
                return short.MinValue;
            return (short)Value;
        }

        public int DivideByRoundUp(int Value, int Divisor)
        {
            return (int)Math.Ceiling((double)Value / Divisor);
        }
    }
}
