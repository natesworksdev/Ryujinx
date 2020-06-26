using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Threading;
using System;

namespace Ryujinx.HLE.HOS.Services.Nifm.StaticService
{
    class IRequest : IpcService
    {
        private KEvent _event0;
        private KEvent _event1;

        private uint _version;

        public IRequest(Horizon system, uint version)
        {
            _event0 = new KEvent(system.KernelContext);
            _event1 = new KEvent(system.KernelContext);

            _version = version;
        }

        [Command(0)]
        // GetRequestState() -> u32
        public ResultCode GetRequestState(ServiceCtx context)
        {
            context.ResponseData.Write(1);

            Logger.PrintStub(LogClass.ServiceNifm);

            return ResultCode.Success;
        }

        [Command(1)]
        // GetResult()
        public ResultCode GetResult(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceNifm);

            return GetResultImpl();
        }

        private ResultCode GetResultImpl()
        {
            return ResultCode.Success;
        }

        [Command(2)]
        // GetSystemEventReadableHandles() -> (handle<copy>, handle<copy>)
        public ResultCode GetSystemEventReadableHandles(ServiceCtx context)
        {
            if (context.Process.HandleTable.GenerateHandle(_event0.ReadableEvent, out int handle0) != KernelResult.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            if (context.Process.HandleTable.GenerateHandle(_event1.ReadableEvent, out int handle1) != KernelResult.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(handle0, handle1);

            return ResultCode.Success;
        }

        [Command(3)]
        // Cancel()
        public ResultCode Cancel(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceNifm);

            return ResultCode.Success;
        }

        [Command(4)]
        // Submit()
        public ResultCode Submit(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceNifm);

            return ResultCode.Success;
        }

        [Command(11)]
        // SetConnectionConfirmationOption(i8)
        public ResultCode SetConnectionConfirmationOption(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceNifm);

            return ResultCode.Success;
        }

        [Command(21)]
        // GetAppletInfo(u32) -> (u32, u32, u32, buffer<bytes, 6>)
        public ResultCode GetAppletInfo(ServiceCtx context)
        {
            uint themeColor = context.RequestData.ReadUInt32();

            Logger.PrintStub(LogClass.ServiceNifm);

            ResultCode result = GetResultImpl();

            if(result == 0 || ((int)result & 0x3fffff) == 0xe06e)
            {
                return (ResultCode)0x1686e;
            }

            // Returns appletId, libraryAppletMode, outSize and a buffer. 
            // Returned applet ids- (0x19, 0xf, 0xe)
            // libraryAppletMode seems to be 0 for all applets supported.

            // TODO: check order
            context.ResponseData.Write(0xe); // Use error applet as default for now
            context.ResponseData.Write(0); // libraryAppletMode
            context.ResponseData.Write(0); // outSize

            return ResultCode.Success;
        }
    }
}