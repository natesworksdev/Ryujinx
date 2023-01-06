using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Time;
using System.Collections.Generic;

namespace Ryujinx.Horizon.Sdk.Timesrv.Detail.Service
{
    class SteadyClock : ISteadyClock
    {
        [CmifCommand(0)]
        public Result GetCurrentTimePoint(out SteadyClockTimePoint arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(2)]
        public Result GetTestOffset(out TimeSpanType arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(3)]
        public Result SetTestOffset(TimeSpanType arg0)
        {
            return Result.Success;
        }

        [CmifCommand(100)]
        public Result GetRtcValue(out long arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(101)]
        public Result IsRtcResetDetected(out bool arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(102)]
        public Result GetSetupResultValue(out uint arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(200)]
        public Result GetInternalOffset(out TimeSpanType arg0)
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