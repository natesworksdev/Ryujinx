using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.Pcv.Bpc;

namespace Ryujinx.HLE.HOS.Services.Time.Clock
{
    class StandardSteadyClockCore : SteadyClockCore
    {
        private TimeSpanType _setupValue;
        // TODO: move this to glue when we will have psc fully done
        private ResultCode   _setupResultCode;
        private TimeSpanType _testOffset;
        private TimeSpanType _internalOffset;
        private TimeSpanType _cachedRawTimePoint;

        public StandardSteadyClockCore()
        {
            _setupValue         = new TimeSpanType(0);
            _testOffset         = new TimeSpanType(0);
            _internalOffset     = new TimeSpanType(0);
            _cachedRawTimePoint = new TimeSpanType(0);
        }

        public override SteadyClockTimePoint GetTimePoint(KThread thread)
        {
            SteadyClockTimePoint result = new SteadyClockTimePoint
            {
                TimePoint     = GetCurrentRawTimePoint(thread).ToSeconds(),
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

        public override TimeSpanType GetCurrentRawTimePoint(KThread thread)
        {
            TimeSpanType ticksTimeSpan = TimeSpanType.FromTicks(thread.Context.CntpctEl0, thread.Context.CntfrqEl0);

            TimeSpanType rawTimePoint = new TimeSpanType(_setupValue.NanoSeconds + ticksTimeSpan.NanoSeconds);

            if (rawTimePoint.NanoSeconds < _cachedRawTimePoint.NanoSeconds)
            {
                rawTimePoint.NanoSeconds = _cachedRawTimePoint.NanoSeconds;
            }

            _cachedRawTimePoint = rawTimePoint;

            return rawTimePoint;
        }

        // TODO: move this to glue when we will have psc fully done
        public void ConfigureSetupValue()
        {
            int retry = 0;

            ResultCode result = ResultCode.Success;

            while (retry < 20)
            {
                result = (ResultCode)IRtcManager.GetExternalRtcValue(out ulong rtcValue);

                if (result == ResultCode.Success)
                {
                    _setupValue = TimeSpanType.FromSeconds((long)rtcValue);
                    break;
                }

                retry++;
            }

            _setupResultCode = result;
        }
    }
}
