using Ryujinx.Core.OsHle.Ipc;
using System;
using System.Collections.Generic;

using static Ryujinx.Core.OsHle.IpcServices.ObjHelper;

namespace Ryujinx.Core.OsHle.IpcServices.Time
{
    class ServiceTime : IIpcService
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        private static DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local);

        public ServiceTime()
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, GetStandardUserSystemClock    },
                { 1, GetStandardNetworkSystemClock },
                { 2, GetStandardSteadyClock        },
                { 3, GetTimeZoneService            },
                { 4, GetStandardLocalSystemClock   }
            };
        }

        public long GetStandardUserSystemClock(ServiceCtx Context)
        {
            MakeObject(Context, new ISystemClock(Epoch, SystemClockType.User));

            return 0;
        }

        public long GetStandardNetworkSystemClock(ServiceCtx Context)
        {
            MakeObject(Context, new ISystemClock(Epoch, SystemClockType.Network));

            return 0;
        }

        public long GetStandardSteadyClock(ServiceCtx Context)
        {
            MakeObject(Context, new ISteadyClock());

            return 0;
        }

        public long GetTimeZoneService(ServiceCtx Context)
        {
            MakeObject(Context, new ITimeZoneService(Epoch));

            return 0;
        }

        public long GetStandardLocalSystemClock(ServiceCtx Context)
        {
            MakeObject(Context, new ISystemClock(Epoch, SystemClockType.Local));

            return 0;
        }

    }
}