using Ryujinx.HLE.HOS.Kernel.Threading;

namespace Ryujinx.HLE.HOS.Services.Time.Clock
{
    class StandardUserSystemClockCore : SystemClockCore
    {
        private StandardLocalSystemClockCore   _localSystemClockCore;
        private StandardNetworkSystemClockCore _networkSystemClockCore;
        private bool                           _autoCorrectionEnabled;

        public StandardUserSystemClockCore(StandardLocalSystemClockCore localSystemClockCore, StandardNetworkSystemClockCore networkSystemClockCore) : base(localSystemClockCore.GetSteadyClockCore())
        {
            _localSystemClockCore   = localSystemClockCore;
            _networkSystemClockCore = networkSystemClockCore;
            _autoCorrectionEnabled  = false;
        }

        protected override ResultCode Flush(SystemClockContext context)
        {
            return ResultCode.NotImplemented;
        }

        public override ResultCode GetClockContext(KThread thread, out SystemClockContext context)
        {
            ResultCode result = ApplyAutomaticCorrection(thread, false);

            context = new SystemClockContext();

            if (result == ResultCode.Success)
            {
                return _localSystemClockCore.GetClockContext(thread, out context);
            }

            return result;
        }

        public override ResultCode SetClockContext(SystemClockContext context)
        {
            return ResultCode.NotImplemented;
        }

        private ResultCode ApplyAutomaticCorrection(KThread thread, bool autoCorrectionEnabled)
        {
            ResultCode result = ResultCode.Success;

            if (_autoCorrectionEnabled != autoCorrectionEnabled && _networkSystemClockCore.IsClockSetup(thread))
            {
                result = _networkSystemClockCore.GetClockContext(thread, out SystemClockContext context);

                if (result == ResultCode.Success)
                {
                    _localSystemClockCore.SetClockContext(context);
                }
            }

            return result;
        }

        public ResultCode SetAutomaticCorrectionEnabled(KThread thread, bool autoCorrectionEnabled)
        {
            ResultCode result = ApplyAutomaticCorrection(thread, autoCorrectionEnabled);

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
    }
}
