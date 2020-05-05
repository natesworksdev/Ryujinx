using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Threading;
using System;

namespace Ryujinx.HLE.HOS.Services.Ns
{
    [Service("aoc:u")]
    class IAddOnContentManager : IpcService
    {
        private KEvent _listChangedEvent;

        public IAddOnContentManager(ServiceCtx context)
        {
            // This is signaled when the AddOnContent list has changed to reloaded it while the game is running.
            _listChangedEvent = new KEvent(context.Device.System.KernelContext);
        }

        [Command(2)]
        // CountAddOnContent(u64, pid) -> u32
        public static ResultCode CountAddOnContent(ServiceCtx context)
        {
            context.ResponseData.Write(0);

            Logger.PrintStub(LogClass.ServiceNs);

            return ResultCode.Success;
        }

        [Command(3)]
        // ListAddOnContent(u32, u32, u64, pid) -> (u32, buffer<u32, 6>)
        public static ResultCode ListAddOnContent(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceNs);

            // TODO: This is supposed to write a u32 array aswell.
            // It's unknown what it contains.
            context.ResponseData.Write(0);

            return ResultCode.Success;
        }

        [Command(8)] // 4.0.0+
        // GetAddOnContentListChangedEvent() -> handle<copy>
        public ResultCode GetAddOnContentListChangedEvent(ServiceCtx context)
        {
            if (context.Process.HandleTable.GenerateHandle(_listChangedEvent.ReadableEvent, out int handle) != KernelResult.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(handle);

            return ResultCode.Success;
        }
    }
}