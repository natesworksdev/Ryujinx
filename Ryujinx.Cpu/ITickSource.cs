using System;

namespace Ryujinx.Cpu
{
    public interface ITickSource
    {
        ulong Frequency { get; }
        ulong Counter { get; }

        TimeSpan ElapsedTime { get; }
        double ElapsedSeconds { get; }

        void Suspend();
        void Resume();
    }
}
