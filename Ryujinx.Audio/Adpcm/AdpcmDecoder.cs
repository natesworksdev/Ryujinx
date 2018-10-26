namespace Ryujinx.Audio.Adpcm
{
    public static class AdpcmDecoder
    {
        private const int SamplesPerFrame = 14;
        private const int BytesPerFrame   = 8;

        public static int[] Decode(byte[] buffer, AdpcmDecoderContext context)
        {
            int samples = GetSamplesCountFromSize(buffer.Length);

            int[] pcm = new int[samples * 2];

            short history0 = context.History0;
            short history1 = context.History1;

            int inputOffset  = 0;
            int outputOffset = 0;

            while (inputOffset < buffer.Length)
            {
                byte header = buffer[inputOffset++];

                int scale = 0x800 << (header & 0xf);

                int coeffIndex = (header >> 4) & 7;

                short coeff0 = context.Coefficients[coeffIndex * 2 + 0];
                short coeff1 = context.Coefficients[coeffIndex * 2 + 1];

                int frameSamples = SamplesPerFrame;

                if (frameSamples > samples) frameSamples = samples;

                int value = 0;

                for (int sampleIndex = 0; sampleIndex < frameSamples; sampleIndex++)
                {
                    int sample;

                    if ((sampleIndex & 1) == 0)
                    {
                        value = buffer[inputOffset++];

                        sample = (value << 24) >> 28;
                    }
                    else
                    {
                        sample = (value << 28) >> 28;
                    }

                    int prediction = coeff0 * history0 + coeff1 * history1;

                    sample = (sample * scale + prediction + 0x400) >> 11;

                    short saturatedSample = DspUtils.Saturate(sample);

                    history1 = history0;
                    history0 = saturatedSample;

                    pcm[outputOffset++] = saturatedSample;
                    pcm[outputOffset++] = saturatedSample;
                }

                samples -= frameSamples;
            }

            context.History0 = history0;
            context.History1 = history1;

            return pcm;
        }

        public static long GetSizeFromSamplesCount(int samplesCount)
        {
            int frames = samplesCount / SamplesPerFrame;

            return frames * BytesPerFrame;
        }

        public static int GetSamplesCountFromSize(long size)
        {
            int frames = (int)(size / BytesPerFrame);

            return frames * SamplesPerFrame;
        }
    }
}