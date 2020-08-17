using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Services.OsTypes;
using System;

namespace Ryujinx.HLE.HOS.Services.Ns
{
    class IPurchaseEventManager : IpcService, IDisposable
    {
        private SystemEventType _purchasedEvent;

        public IPurchaseEventManager(Horizon system)
        {
            Os.CreateSystemEvent(out _purchasedEvent, EventClearMode.AutoClear, true);
        }

        [Command(0)]
        // SetDefaultDeliveryTarget(pid, buffer<bytes, 5> unknown)
        public ResultCode SetDefaultDeliveryTarget(ServiceCtx context)
        {
            long   inBufferPosition = context.Request.SendBuff[0].Position;
            long   inBufferSize     = context.Request.SendBuff[0].Size;
            byte[] buffer           = new byte[inBufferSize];

            context.Memory.Read((ulong)inBufferPosition, buffer);

            // NOTE: Service use the pid to call arp:r GetApplicationLaunchProperty and store it in internal field.
            //       Then it seems to use the buffer content and compare it with a stored linked instrusive list.
            //       Since we don't support purchase from eShop, we can stub it.

            Logger.Stub?.PrintStub(LogClass.ServiceNs);

            return ResultCode.Success;
        }

        [Command(2)]
        // GetPurchasedEventReadableHandle() -> handle<copy, event>
        public ResultCode GetPurchasedEventReadableHandle(ServiceCtx context)
        {
            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(Os.GetReadableHandleOfSystemEvent(ref _purchasedEvent));

            return ResultCode.Success;
        }

        public void Dispose()
        {
            Os.DestroySystemEvent(ref _purchasedEvent);
        }
    }
}