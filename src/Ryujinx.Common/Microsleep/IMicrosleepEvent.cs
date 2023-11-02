using System;
using System.Threading;

namespace Ryujinx.Common.Microsleep
{
    public interface IMicrosleepEvent : IDisposable
    {
        bool CanSleepTil(long timePoint);

        long AdjustTimePoint(long timePoint);

        bool SleepUntil(long timePoint, bool strictlyBefore = false);

        void Sleep();

        void Signal();
    }
}
