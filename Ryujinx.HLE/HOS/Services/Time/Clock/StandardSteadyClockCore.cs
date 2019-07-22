using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.Bpc;
using Ryujinx.HLE.Utilities;
using System;

namespace Ryujinx.HLE.HOS.Services.Time.Clock
{
    class StandardSteadyClockCore : SteadyClockCore
    {
        private long         _setupValue;
        private ResultCode   _setupResultCode;
        private bool         _isRtcResetDetected;
        private TimeSpanType _testOffset;
        private TimeSpanType _internalOffset;
        private UInt128      _clockSourceId;

        private static StandardSteadyClockCore instance;

        public static StandardSteadyClockCore Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new StandardSteadyClockCore();
                }

                return instance;
            }
        }

        private StandardSteadyClockCore()
        {
            _testOffset     = new TimeSpanType(0);
            _internalOffset = new TimeSpanType(0);
            _clockSourceId  = new UInt128(Guid.NewGuid().ToByteArray());
        }

        public override SteadyClockTimePoint GetTimePoint(KThread thread)
        {
            SteadyClockTimePoint result = new SteadyClockTimePoint
            {
                TimePoint     = 0,
                ClockSourceId = _clockSourceId
            };

            TimeSpanType ticksTimeSpan = TimeSpanType.FromTicks(thread.Context.ThreadState.CntpctEl0, thread.Context.ThreadState.CntfrqEl0);

            result.TimePoint = _setupValue + ticksTimeSpan.ToSeconds();

            return result;
        }

        public UInt128 GetClockSourceId()
        {
            return _clockSourceId;
        }

        public override TimeSpanType GetTestOffset()
        {
            return _testOffset;
        }

        public override void SetTestOffset(TimeSpanType testOffset)
        {
            _testOffset = testOffset;
        }

        public override ResultCode GetRtcValue(out ulong rtcValue)
        {
            return (ResultCode)IRtcManager.GetExternalRtcValue(out rtcValue);
        }

        public bool IsRtcResetDetected()
        {
            return _isRtcResetDetected;
        }

        public override TimeSpanType GetInternalOffset()
        {
            return _internalOffset;
        }

        public override void SetInternalOffset(TimeSpanType internalOffset)
        {
            _internalOffset = internalOffset;
        }

        public override ResultCode GetSetupResultValue()
        {
            return _setupResultCode;
        }

        public void ConfigureSetupValue()
        {
            int retry = 0;

            ResultCode result = ResultCode.Success;

            while (retry < 20)
            {
                result = (ResultCode)IRtcManager.GetExternalRtcValue(out ulong rtcValue);

                if (result == ResultCode.Success)
                {
                    _setupValue = (long)rtcValue;
                    break;
                }

                retry++;
            }

            _setupResultCode = result;
        }
    }
}
