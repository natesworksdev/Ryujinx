using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.Profiler
{
    public enum TimingFlagType
    {
        FrameSwap,
    }

    public struct TimingFlag
    {
        public TimingFlagType FlagType;
        public long Timestamp;
    }
}
