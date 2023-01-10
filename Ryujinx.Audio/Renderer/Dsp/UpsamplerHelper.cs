using Ryujinx.Audio.Renderer.Server.Upsampler;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ryujinx.Audio.Renderer.Dsp
{
    public class UpsamplerHelper
    {
        private const int HistoryLength = UpsamplerBufferState.HistoryLength;
        private const int FilterBankLength = 20;
        // Bank0 = [0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        private const int Bank0CenterIndex = 9;
        private static readonly float[] Bank1 = PrecomputeFilterBank(1.0f / 6.0f);
        private static readonly float[] Bank2 = PrecomputeFilterBank(2.0f / 6.0f);
        private static readonly float[] Bank3 = PrecomputeFilterBank(3.0f / 6.0f);
        private static readonly float[] Bank4 = PrecomputeFilterBank(4.0f / 6.0f);
        private static readonly float[] Bank5 = PrecomputeFilterBank(5.0f / 6.0f);

        private static float[] PrecomputeFilterBank(float offset)
        {
            float Sinc(float x)
            {
                if (x == 0)
                {
                    return 1.0f;
                }
                return (float) (Math.Sin(Math.PI * x) / (Math.PI * x));
            }

            float BlackmanWindow(float x)
            {
                const float a = 0.18f;
                const float a0 = 0.5f - 0.5f * a;
                const float a1 = -0.5f;
                const float a2 = 0.5f * a;
                return a0 + a1 * (float)Math.Cos(2 * Math.PI * x) + a2 * (float)Math.Cos(4 * Math.PI * x);
            }
            
            float[] result = new float[FilterBankLength];

            for (int i = 0; i < FilterBankLength; i++)
            {
                float x = (Bank0CenterIndex - i) + offset;
                result[i] = Sinc(x) * BlackmanWindow(x / FilterBankLength + 0.5f);
            }

            return result;
        }

        // Polyphase upsampling algorithm
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Upsample(Span<float> outputBuffer, ReadOnlySpan<float> inputBuffer, int outputSampleCount, int inputSampleCount, ref UpsamplerBufferState state)
        {
            if (!state.Initialized)
            {
                state.Scale = inputSampleCount switch
                {
                    40  => 6.0f,
                    80  => 3.0f,
                    160 => 1.5f,
                    _   => throw new ArgumentOutOfRangeException()
                };
                state.Initialized = true;
            }

            if (outputSampleCount == 0)
            {
                return;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            float DoFilterBank(ref UpsamplerBufferState state, float[] bank)
            {
                float result = 0.0f;

                Debug.Assert(state.History.Length == HistoryLength);
                Debug.Assert(bank.Length == FilterBankLength);
                for (int j = 0; j < FilterBankLength; j++)
                {
                    result += bank[j] * state.History[j];
                }

                return result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void NextInput(ref UpsamplerBufferState state, float input)
            {
                Array.Copy(state.History, 1, state.History, 0, HistoryLength - 1);
                state.History[HistoryLength - 1] = input;
            }

            int inputBufferIndex = 0;

            switch (state.Scale)
            { 
                case 6.0f:
                    for (int i = 0; i < outputSampleCount; i++)
                    {
                        switch (state.Phase)
                        {
                            case 0:
                                NextInput(ref state, inputBuffer[inputBufferIndex++]);
                                outputBuffer[i] = state.History[Bank0CenterIndex];
                                break;
                            case 1:
                                outputBuffer[i] = DoFilterBank(ref state, Bank1);
                                break;
                            case 2:
                                outputBuffer[i] = DoFilterBank(ref state, Bank2);
                                break;
                            case 3:
                                outputBuffer[i] = DoFilterBank(ref state, Bank3);
                                break;
                            case 4:
                                outputBuffer[i] = DoFilterBank(ref state, Bank4);
                                break;
                            case 5:
                                outputBuffer[i] = DoFilterBank(ref state, Bank5);
                                break;
                        }

                        state.Phase = (state.Phase + 1) % 6;
                    }
                    break;
                case 3.0f:
                    for (int i = 0; i < outputSampleCount; i++)
                    {
                        switch (state.Phase)
                        {
                            case 0:
                                NextInput(ref state, inputBuffer[inputBufferIndex++]);
                                outputBuffer[i] = state.History[Bank0CenterIndex];
                                break;
                            case 1:
                                outputBuffer[i] = DoFilterBank(ref state, Bank2);
                                break;
                            case 2:
                                outputBuffer[i] = DoFilterBank(ref state, Bank4);
                                break;
                        }

                        state.Phase = (state.Phase + 1) % 3;
                    }
                    break;
                case 1.5f:
                    // Upsample by 3 then decimate by 2.
                    for (int i = 0; i < outputSampleCount; i++)
                    {
                        switch (state.Phase)
                        {
                            case 0:
                                NextInput(ref state, inputBuffer[inputBufferIndex++]);
                                outputBuffer[i] = state.History[Bank0CenterIndex];
                                break;
                            case 1:
                                outputBuffer[i] = DoFilterBank(ref state, Bank4);
                                break;
                            case 2:
                                NextInput(ref state, inputBuffer[inputBufferIndex++]);
                                outputBuffer[i] = DoFilterBank(ref state, Bank2);
                                break;
                        }

                        state.Phase = (state.Phase + 1) % 3;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}