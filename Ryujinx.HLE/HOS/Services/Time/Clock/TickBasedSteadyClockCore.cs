using Ryujinx.HLE.HOS.Kernel.Threading;

namespace Ryujinx.HLE.HOS.Services.Time.Clock
{
    class TickBasedSteadyClockCore : SteadyClockCore
    {
        public TickBasedSteadyClockCore() {}

        public override SteadyClockTimePoint GetTimePoint()
        {
            SteadyClockTimePoint result = new SteadyClockTimePoint
            {
                TimePoint     = 0,
                ClockSourceId = GetClockSourceId()
            };

            TimeSpanType ticksTimeSpan = TimeSpanType.FromTimeSpan(ARMeilleure.State.ExecutionContext.ElapsedTime);

            result.TimePoint = ticksTimeSpan.ToSeconds();

            return result;
        }
    }
}
