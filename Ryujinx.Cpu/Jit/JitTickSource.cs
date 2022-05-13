using ARMeilleure.State;
using System;
using System.Diagnostics;

namespace Ryujinx.Cpu.Jit
{
    class JitTickSource : ITickSource, ICounter
    {
        private static Stopwatch _tickCounter;

        private static double _hostTickFreq;

        public ulong Frequency { get; }
        public ulong Counter
        {
            get
            {
                return (ulong)(ElapsedSeconds * Frequency);
            }
        }

        public TimeSpan ElapsedTime => _tickCounter.Elapsed;
        public double ElapsedSeconds => _tickCounter.ElapsedTicks * _hostTickFreq;

        public JitTickSource(ulong frequency)
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