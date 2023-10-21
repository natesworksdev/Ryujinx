using Ryujinx.Horizon.Common;
using System;

namespace Ryujinx.Horizon.Sdk.Am
{
    public interface IWindowController
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
