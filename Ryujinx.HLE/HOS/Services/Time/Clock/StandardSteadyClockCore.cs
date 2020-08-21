namespace Ryujinx.HLE.HOS.Services.Time.Clock
{
    class StandardSteadyClockCore : SteadyClockCore
    {
        private TimeSpanType _setupValue;
        private TimeSpanType _testOffset;
        private TimeSpanType _internalOffset;
        private TimeSpanType _cachedRawTimePoint;

        public StandardSteadyClockCore()
        {
            _setupValue         = TimeSpanType.Zero;
            _testOffset         = TimeSpanType.Zero;
            _internalOffset     = TimeSpanType.Zero;
            _cachedRawTimePoint = TimeSpanType.Zero;
        }

        public override SteadyClockTimePoint GetTimePoint()
        {
            SteadyClockTimePoint result = new SteadyClockTimePoint
            {
                TimePoint     = GetCurrentRawTimePoint().ToSeconds(),
                ClockSourceId = GetClockSourceId()
            };

            return result;
        }

        public override TimeSpanType GetTestOffset()
        {
            return _testOffset;
        }

        public override void SetTestOffset(TimeSpanType testOffset)
        {
            _testOffset = testOffset;
        }

        public override TimeSpanType GetInternalOffset()
        {
            return _internalOffset;
        }

        public override void SetInternalOffset(TimeSpanType internalOffset)
        {
            _internalOffset = internalOffset;
        }

        public override TimeSpanType GetCurrentRawTimePoint()
        {
            TimeSpanType ticksTimeSpan = TimeSpanType.FromTimeSpan(ARMeilleure.State.ExecutionContext.ElapsedTime);

            TimeSpanType rawTimePoint = new TimeSpanType(_setupValue.NanoSeconds + ticksTimeSpan.NanoSeconds);

            if (rawTimePoint.NanoSeconds < _cachedRawTimePoint.NanoSeconds)
            {
                rawTimePoint.NanoSeconds = _cachedRawTimePoint.NanoSeconds;
            }

            _cachedRawTimePoint = rawTimePoint;

            return rawTimePoint;
        }

        public void SetSetupValue(TimeSpanType setupValue)
        {
            _setupValue = setupValue;
        }
    }
}
