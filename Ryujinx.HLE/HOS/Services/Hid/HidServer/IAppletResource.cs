using Ryujinx.HLE.HOS.Ipc;

namespace Ryujinx.HLE.HOS.Services.Hid.HidServer
{
    class IAppletResource : IpcService
    {
        public IAppletResource()
        {
        }

        [Command(0)]
        // GetSharedMemoryHandle() -> handle<copy>
        public ResultCode GetSharedMemoryHandle(ServiceCtx context)
        {
            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(context.Device.System.ServiceServer.HidServer.HidSharedMemoryHandle);

            return ResultCode.Success;
        }
    }
}