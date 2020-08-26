using Ryujinx.Horizon.Sdk.OsTypes;
using System;

namespace Ryujinx.HLE.HOS.Services.Time.Clock
{
    class StandardUserSystemClockCore : SystemClockCore
    {
        private StandardLocalSystemClockCore   _localSystemClockCore;
        private StandardNetworkSystemClockCore _networkSystemClockCore;
        private bool                           _autoCorrectionEnabled;
        private SteadyClockTimePoint           _autoCorrectionTime;
        private SystemEventType                _autoCorrectionEvent;

        public StandardUserSystemClockCore(StandardLocalSystemClockCore localSystemClockCore, StandardNetworkSystemClockCore networkSystemClockCore) : base(localSystemClockCore.GetSteadyClockCore())
        {
            _localSystemClockCore   = localSystemClockCore;
            _networkSystemClockCore = networkSystemClockCore;
            _autoCorrectionEnabled  = false;
            _autoCorrectionTime     = SteadyClockTimePoint.GetRandom();
        }

        protected override ResultCode Flush(SystemClockContext context)
        {
            // As UserSystemClock isn't a real system clock, this shouldn't happens.
            throw new NotImplementedException();
        }

        public override ResultCode GetClockContext(out SystemClockContext context)
        {
            ResultCode result = ApplyAutomaticCorrection(false);

            context = new SystemClockContext();

            if (result == ResultCode.Success)
            {
                return _localSystemClockCore.GetClockContext(out context);
            }

            return result;
        }

        public override ResultCode SetClockContext(SystemClockContext context)
        {
            return ResultCode.NotImplemented;
        }

        private ResultCode ApplyAutomaticCorrection(bool autoCorrectionEnabled)
        {
            ResultCode result = ResultCode.Success;

            if (_autoCorrectionEnabled != autoCorrectionEnabled && _networkSystemClockCore.IsClockSetup())
            {
                result = _networkSystemClockCore.GetClockContext(out SystemClockContext context);

                if (result == ResultCode.Success)
                {
                    _localSystemClockCore.SetClockContext(context);
                }
            }

            return result;
        }

        internal void CreateAutomaticCorrectionEvent()
        {
            Os.CreateSystemEvent(out _autoCorrectionEvent, EventClearMode.AutoClear, true);
        }

        public ResultCode SetAutomaticCorrectionEnabled(bool autoCorrectionEnabled)
        {
            ResultCode result = ApplyAutomaticCorrection(autoCorrectionEnabled);

            if (result == ResultCode.Success)
            {
                _autoCorrectionEnabled = autoCorrectionEnabled;
            }

            return result;
        }

        public bool IsAutomaticCorrectionEnabled()
        {
            return _autoCorrectionEnabled;
        }

        public int GetAutomaticCorrectionReadableEventHandle()
        {
            return Os.GetReadableHandleOfSystemEvent(ref _autoCorrectionEvent);
        }

        public void SetAutomaticCorrectionUpdatedTime(SteadyClockTimePoint steadyClockTimePoint)
        {
            _autoCorrectionTime = steadyClockTimePoint;
        }

        public SteadyClockTimePoint GetAutomaticCorrectionUpdatedTime()
        {
            return _autoCorrectionTime;
        }

        public void SignalAutomaticCorrectionEvent()
        {
            Os.SignalSystemEvent(ref _autoCorrectionEvent);
        }
    }
}
