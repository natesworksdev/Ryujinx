using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;
using System;

namespace Ryujinx.Horizon.Sdk.Am.Controllers
{
    interface IWindowController : IServiceObject
    {
        Result CreateWindow();
        Result GetAppletResourceUserId();
        Result GetAppletResourceUserIdOfCallerApplet();
        Result AcquireForegroundRights();
        Result ReleaseForegroundRights();
        Result RejectToChangeIntoBackground();
        Result SetAppletWindowVisibility(bool visibility);
        Result SetAppletGpuTimeSlice(Int64 gpuTime);
    }
}
