using System;

namespace Ryujinx.Audio.ADPCM
{
    class Decoder
    {
        private const int SamplesPerFrame = 14;

        private AudioHelper Helper;

        public Decoder()
        {
            Helper = new AudioHelper();
        }

        public short[] Decode(byte[] ADPCM, ADPCMInfo Info, int Samples)
        {
            short[] PCM = new short[Samples];

            short   Hist1        = Info.History1;
            short   Hist2        = Info.History2;
            short[] Coefficients = Info.Coefficients;

            int FrameCount       = Helper.DivideByRoundUp(Samples, SamplesPerFrame);
            int SamplesRemaining = Samples;

            int OutIndex = 0;
            int InIndex  = 0;

            for (int Index = 0; Index < FrameCount; Index++)
            {
                byte PredictorScale = ADPCM[InIndex++];
                int  Scale          = (1 << Helper.GetLowNibble(PredictorScale)) * 2048;
                int  Predictor      = Helper.GetHighNibble(PredictorScale);

                short Coef1 = Info.Coefficients[Predictor * 2];
                short Coef2 = Info.Coefficients[Predictor * 2 + 1];

                int SamplesToRead = Math.Min(SamplesPerFrame, SamplesRemaining);

                for (int SampleIndex = 0; SampleIndex < SamplesToRead; SampleIndex++)
                {
                    int ADPCMSample = SampleIndex % 2 == 0 ? Helper.GetHighNibble(ADPCM[InIndex]) : Helper.GetLowNibble(ADPCM[InIndex++]);
                    ADPCMSample   <<= 28;
                    ADPCMSample   >>= 28;

                    int   Distance        = Scale * ADPCMSample;
                    int   PredictedSample = Coef1 * Hist1 + Coef2 * Hist2;
                    int   CorrectedSample = PredictedSample + Distance;
                    int   ScaledSample    = (CorrectedSample + 1024) >> 11;
                    short ClampedSample   = Helper.Clamp16(ScaledSample);

                    Hist2 = Hist1;
                    Hist1 = ClampedSample;

                    PCM[OutIndex++] = ClampedSample;
                }

                SamplesRemaining -= SamplesToRead;
            }

            return PCM;
        }
    }
}
