using Ryujinx.Common.Memory;
using System.Runtime.InteropServices;

namespace Ryujinx.Audio.Renderer.Parameter.Effect
{
    /// <summary>
    /// Effect result state for <seealso cref="Common.EffectType.Limiter"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CompressorStatistics
    {
        public float MaximumMean;
        public float MinimumGain;
        public Array6<float> LastSamples;

        /// <summary>
        /// Reset the statistics.
        /// </summary>
        /// <param name="channelCount">Number of channels to reset.</param>
        public void Reset(ushort channelCount)
        {
            MaximumMean = 0.0f;
            MinimumGain = 1.0f;
            LastSamples.AsSpan()[..channelCount].Clear();
        }
    }
}
