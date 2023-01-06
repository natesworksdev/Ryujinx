using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Time;

namespace Ryujinx.Horizon.Sdk.Timesrv.Detail.Service
{
    interface ISystemClock : IServiceObject
    {
        Result GetCurrentTime(out PosixTime arg0);
        Result SetCurrentTime(PosixTime arg0);
        Result GetSystemClockContext(out SystemClockContext arg0);
        Result SetSystemClockContext(SystemClockContext arg0);
        Result GetOperationEventReadableHandle(out int arg0);
    }
}
