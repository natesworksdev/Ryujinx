using Ryujinx.HLE.HOS.Ipc;
using System;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Time
{
    class StaticService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        private static readonly DateTime StartupDate = DateTime.UtcNow;

        public StaticService()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0,   GetStandardUserSystemClock                 },
                { 1,   GetStandardNetworkSystemClock              },
                { 2,   GetStandardSteadyClock                     },
                { 3,   GetTimeZoneService                         },
                { 4,   GetStandardLocalSystemClock                },
                { 300, CalculateMonotonicSystemClockBaseTimePoint }
            };
        }

        public long GetStandardUserSystemClock(ServiceCtx context)
        {
            MakeObject(context, new SystemClock(SystemClockType.User));

            return 0;
        }

        public long GetStandardNetworkSystemClock(ServiceCtx context)
        {
            MakeObject(context, new SystemClock(SystemClockType.Network));

            return 0;
        }

        public long GetStandardSteadyClock(ServiceCtx context)
        {
            MakeObject(context, new SteadyClock());

            return 0;
        }

        public long GetTimeZoneService(ServiceCtx context)
        {
            MakeObject(context, new TimeZoneService());

            return 0;
        }

        public long GetStandardLocalSystemClock(ServiceCtx context)
        {
            MakeObject(context, new SystemClock(SystemClockType.Local));

            return 0;
        }

        public long CalculateMonotonicSystemClockBaseTimePoint(ServiceCtx context)
        {
            long timeOffset              = (long)(DateTime.UtcNow - StartupDate).TotalSeconds;
            long systemClockContextEpoch = context.RequestData.ReadInt64();

            context.ResponseData.Write(timeOffset + systemClockContextEpoch);

            return 0;
        }

    }
}