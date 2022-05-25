using ARMeilleure.State;
using System;

namespace Ryujinx.Cpu
{
    public interface ITickSource : ICounter
    {
        TimeSpan ElapsedTime { get; }
        double ElapsedSeconds { get; }

        void Suspend();
        void Resume();
    }
}
