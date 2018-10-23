namespace Ryujinx.Audio.Adpcm
{
    public static class DspUtils
    {
        public static short Saturate(int value)
        {
            if (value > short.MaxValue)
                value = short.MaxValue;

            if (value < short.MinValue)
                value = short.MinValue;

            return (short)value;
        }
    }
}