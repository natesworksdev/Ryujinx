using System;
using System.Threading;

namespace Ryujinx.Common.Microsleep
{
    public interface IMicrosleepEvent : IDisposable
    {
        long AdjustTimePoint(long timePoint, long timeoutNs);

        bool SleepUntil(long timePoint, bool strictlyBefore = false);

        void Sleep();

        void Signal();
    }
}
