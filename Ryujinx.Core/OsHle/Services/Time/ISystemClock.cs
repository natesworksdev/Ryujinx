using Ryujinx.Core.OsHle.Ipc;
using System;
using System.Collections.Generic;

namespace Ryujinx.Core.OsHle.IpcServices.Time
{
    class ISystemClock : IIpcService
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        private DateTime Epoch;

        private SystemClockType ClockType;

        public ISystemClock(DateTime Epoch, SystemClockType ClockType)
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, GetCurrentTime }
            };

            this.Epoch     = Epoch;
            this.ClockType = ClockType;
        }

        public long GetCurrentTime(ServiceCtx Context)
        {
            DateTime CurrentTime = DateTime.Now;

            if (ClockType == SystemClockType.User ||
                ClockType == SystemClockType.Network)
            {
                CurrentTime = CurrentTime.ToUniversalTime();
            }

            Context.ResponseData.Write((long)(DateTime.Now - Epoch).TotalSeconds);

            return 0;
        }
    }
}