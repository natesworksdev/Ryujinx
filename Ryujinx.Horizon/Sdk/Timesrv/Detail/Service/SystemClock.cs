using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Time;
using System.Collections.Generic;

namespace Ryujinx.Horizon.Sdk.Timesrv.Detail.Service
{
    class SystemClock : ISystemClock
    {
        [CmifCommand(0)]
        public Result GetCurrentTime(out PosixTime arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result SetCurrentTime(PosixTime arg0)
        {
            return Result.Success;
        }

        [CmifCommand(2)]
        public Result GetSystemClockContext(out SystemClockContext arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(3)]
        public Result SetSystemClockContext(SystemClockContext arg0)
        {
            return Result.Success;
        }

        [CmifCommand(4)]
        public Result GetOperationEventReadableHandle([CopyHandle] out int arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        public IReadOnlyDictionary<int, CommandHandler> GetCommandHandlers()
        {
            throw new System.NotImplementedException();
        }
    }
}