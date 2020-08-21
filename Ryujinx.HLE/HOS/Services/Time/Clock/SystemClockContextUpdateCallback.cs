using Ryujinx.Horizon.Kernel;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Time.Clock
{
    abstract class SystemClockContextUpdateCallback
    {
        private   List<SignalableEvent> _operationEventList;
        protected SystemClockContext    _context;
        private   bool                  _hasContext;

        public SystemClockContextUpdateCallback()
        {
            _operationEventList = new List<SignalableEvent>();
            _context            = new SystemClockContext();
            _hasContext         = false;
        }

        private bool NeedUpdate(SystemClockContext context)
        {
            if (_hasContext)
            {
                return _context.Offset != context.Offset || _context.SteadyTimePoint.ClockSourceId != context.SteadyTimePoint.ClockSourceId;
            }

            return true;
        }

        public void RegisterOperationEvent(SignalableEvent signalableEvent)
        {
            lock (_operationEventList)
            {
                _operationEventList.Add(signalableEvent);
            }
        }

        private void BroadcastOperationEvent()
        {
            lock (_operationEventList)
            {
                foreach (SignalableEvent e in _operationEventList)
                {
                    e.Signal();
                }
            }
        }

        protected abstract ResultCode Update();

        public ResultCode Update(SystemClockContext context)
        {
            ResultCode result = ResultCode.Success;

            if (NeedUpdate(context))
            {
                _context    = context;
                _hasContext = true;

                result = Update();

                if (result == ResultCode.Success)
                {
                    BroadcastOperationEvent();
                }
            }

            return result;
        }
    }
}
