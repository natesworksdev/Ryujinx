using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am;
using Ryujinx.Horizon.Sdk.Sf;

namespace Ryujinx.Horizon.Am.Ipc.Controllers
{
    partial class WindowController : IWindowController
    {
        [CmifCommand(0)]
        public Result CreateWindow()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(1)]
        public Result GetAppletResourceUserId()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(2)]
        public Result GetAppletResourceUserIdOfCallerApplet()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(10)]
        public Result AcquireForegroundRights()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(11)]
        public Result ReleaseForegroundRights()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(12)]
        public Result RejectToChangeIntoBackground()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(20)]
        public Result SetAppletWindowVisibility(bool visibility)
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(21)]
        public Result SetAppletGpuTimeSlice(long gpuTime)
        {
            throw new System.NotImplementedException();
        }
    }
}
