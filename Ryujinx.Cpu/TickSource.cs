using System;
using System.Diagnostics;

namespace Ryujinx.Cpu
{
    public class TickSource : ITickSource
    {
        private static Stopwatch _tickCounter;

        private static double _hostTickFreq;

        public ulong Frequency { get; }
        public ulong Counter => (ulong)(ElapsedSeconds * Frequency);

        public TimeSpan ElapsedTime => _tickCounter.Elapsed;
        public double ElapsedSeconds => _tickCounter.ElapsedTicks * _hostTickFreq;

        public TickSource(ulong frequency)
        {
            Frequency = frequency;
            _hostTickFreq = 1.0 / Stopwatch.Frequency;

            _tickCounter = new Stopwatch();
            _tickCounter.Start();
        }

        public void Suspend()
        {
            _tickCounter.Stop();
        }

        public void Resume()
        {
            _tickCounter.Start();
        }
    }
}