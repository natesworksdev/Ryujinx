using LibHac;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Threading;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.HLE.HOS.Services.Account.Acc
{
    class IAsyncContext : IpcService
    {
        private KEvent _event;
        public IAsyncContext(ServiceCtx context)
        {
            _event = new KEvent(context.Device.System);
        }

        [Command(0)]
        //GetSystemEvent() -> handle<copy>
        public ResultCode GetSystemEvent(ServiceCtx context)
        {
            if (context.Process.HandleTable.GenerateHandle(_event.ReadableEvent, out int _handle) != KernelResult.Success)
            {
                throw new InvalidOperationException("Out of Handles!");
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(_handle);

            return ResultCode.Success;
        }

        [Command(1)]
        //Cancel()
        public ResultCode Cancel(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceAcc);

            return ResultCode.Success;
        }

        [Command(2)]
        //HasDone() -> b8
        public ResultCode HasDone(ServiceCtx context)
        {
            context.ResponseData.Write(1);

            return ResultCode.Success;
        }

        [Command(3)]
        //GetResult()
        public ResultCode GetResult(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceAcc);

            return ResultCode.Success;
        }
    }
}
