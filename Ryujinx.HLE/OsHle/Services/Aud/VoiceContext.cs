using ChocolArm64.Memory;
using Ryujinx.Audio.Adpcm;
using System;

namespace Ryujinx.HLE.OsHle.Services.Aud
{
    class VoiceContext
    {
        private bool Acquired;

        public int  ChannelsCount;
        public int  BufferIndex;
        public long Offset;

        public float Volume;

        public PlayState PlayState;

        public SampleFormat SampleFormat;

        public AdpcmDecoderContext AdpcmCtx;

        public WaveBuffer[] WaveBuffers;

        public VoiceOut OutStatus;

        public bool Playing => Acquired && PlayState == PlayState.Playing;

        public VoiceContext()
        {
            WaveBuffers = new WaveBuffer[4];
        }

        public void SetAcquireState(bool NewState)
        {
            if (Acquired && !NewState)
            {
                //Release.
                Reset();
            }

            Acquired = NewState;
        }

        private void Reset()
        {
            BufferIndex = 0;
            Offset      = 0;

            OutStatus.PlayedSamplesCount     = 0;
            OutStatus.PlayedWaveBuffersCount = 0;
            OutStatus.VoiceDropsCount        = 0;
        }

        public short[] GetBufferData(AMemory Memory, int MaxSamples, out int Samples)
        {
            WaveBuffer Wb = WaveBuffers[BufferIndex];

            long Position = Wb.Position + Offset;

            long MaxSize = Wb.Size - Offset;

            long Size = GetSizeFromSamplesCount(MaxSamples);

            if (Size > MaxSize)
            {
                Size = MaxSize;
            }

            Samples = GetSamplesCountFromSize(Size);

            OutStatus.PlayedSamplesCount += Samples;

            Offset += Size;

            if (Offset == Wb.Size)
            {
                Offset = 0;

                if (Wb.Looping == 0)
                {
                    BufferIndex = (BufferIndex + 1) & 3;
                }

                OutStatus.PlayedWaveBuffersCount++;
            }

            return Decode(Memory.ReadBytes(Position, Size));
        }

        private long GetSizeFromSamplesCount(int SamplesCount)
        {
            if (SampleFormat == SampleFormat.PcmInt16)
            {
                return SamplesCount * sizeof(short) * ChannelsCount;
            }
            else if (SampleFormat == SampleFormat.Adpcm)
            {
                return AdpcmDecoder.GetSizeFromSamplesCount(SamplesCount);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private int GetSamplesCountFromSize(long Size)
        {
            if (SampleFormat == SampleFormat.PcmInt16)
            {
                return (int)(Size / (sizeof(short) * ChannelsCount));
            }
            else if (SampleFormat == SampleFormat.Adpcm)
            {
                return AdpcmDecoder.GetSamplesCountFromSize(Size);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private short[] Decode(byte[] Buffer)
        {
            if (SampleFormat == SampleFormat.PcmInt16)
            {
                int Samples = GetSamplesCountFromSize(Buffer.Length);

                short[] Output = new short[Samples * 2];

                if (ChannelsCount == 1)
                {
                    //Duplicate samples to convert the mono stream to stereo.
                    for (int Offset = 0; Offset < Buffer.Length; Offset += 2)
                    {
                        short Sample = GetShort(Buffer, Offset);

                        Output[Offset + 0] = Sample;
                        Output[Offset + 1] = Sample;
                    }
                }
                else
                {
                    for (int Offset = 0; Offset < Buffer.Length; Offset += 2)
                    {
                        Output[Offset >> 1] = GetShort(Buffer, Offset);
                    }
                }

                return Output;
            }
            else if (SampleFormat == SampleFormat.Adpcm)
            {
                return AdpcmDecoder.Decode(Buffer, AdpcmCtx);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private static short GetShort(byte[] Buffer, int Offset)
        {
            return (short)((Buffer[Offset + 0] << 0) |
                           (Buffer[Offset + 1] << 8));
        }
    }
}
