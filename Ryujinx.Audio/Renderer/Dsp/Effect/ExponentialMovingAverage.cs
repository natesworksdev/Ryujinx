using System.Runtime.CompilerServices;

namespace Ryujinx.Audio.Renderer.Dsp.Effect
{
    public struct ExponentialMovingAverage
    {
        private float _mean;

        public ExponentialMovingAverage(float mean)
        {
            _mean = mean;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Read()
        {
            return _mean;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Update(float value, float alpha)
        {
            _mean += alpha * (value - _mean);

            return _mean;
        }
    }
}
