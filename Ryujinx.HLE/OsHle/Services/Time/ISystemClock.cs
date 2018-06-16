using Ryujinx.HLE.OsHle.Ipc;
using System;
using System.Collections.Generic;

namespace Ryujinx.HLE.OsHle.Services.Time
{
    class ISystemClock : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private SystemClockType ClockType;

        public ISystemClock(SystemClockType ClockType)
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, GetCurrentTime        },
                { 2, GetSystemClockContext }
            };

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
		
        public long GetSystemClockContext(ServiceCtx Context)
        {
            //Raw data dumped from real switch via pegaswitch
            byte[] SystemClockContext = { 0x07, 0x00, 0x19, 0x00, 0x0d, 0xd2, 0xb2, 0x80};
            
            Array.Resize(ref SystemClockContext, 0x20);
            
            for (int Index = 0; Index < 0x20; Index++)
            {
                Context.ResponseData.Write(SystemClockContext[Index]);
            }
        
            return 0;
        }
    }
}
