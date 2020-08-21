using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Services.OsTypes;
using Ryujinx.Horizon.Kernel.Common;
using System;

namespace Ryujinx.HLE.HOS.Services.BluetoothManager.BtmUser
{
    class IBtmUserCore : IpcService, IDisposable
    {
        private SystemEventType _bleScanEvent;
        private SystemEventType _bleConnectionEvent;
        private SystemEventType _bleServiceDiscoveryEvent;
        private SystemEventType _bleMtuConfigEvent;

        public IBtmUserCore()
        {
            Os.CreateSystemEvent(out _bleScanEvent, EventClearMode.AutoClear, true);
            Os.CreateSystemEvent(out _bleConnectionEvent, EventClearMode.AutoClear, true);
            Os.CreateSystemEvent(out _bleServiceDiscoveryEvent, EventClearMode.AutoClear, true);
            Os.CreateSystemEvent(out _bleMtuConfigEvent, EventClearMode.AutoClear, true);
        }

        [Command(0)] // 5.0.0+
        // AcquireBleScanEvent() -> (byte<1>, handle<copy>)
        public ResultCode AcquireBleScanEvent(ServiceCtx context)
        {
            KernelResult result = KernelResult.Success;

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(Os.GetReadableHandleOfSystemEvent(ref _bleScanEvent));

            context.ResponseData.Write(result == KernelResult.Success ? 1 : 0);

            return ResultCode.Success;
        }

        [Command(17)] // 5.0.0+
        // AcquireBleConnectionEvent() -> (byte<1>, handle<copy>)
        public ResultCode AcquireBleConnectionEvent(ServiceCtx context)
        {
            KernelResult result = KernelResult.Success;

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(Os.GetReadableHandleOfSystemEvent(ref _bleConnectionEvent));

            context.ResponseData.Write(result == KernelResult.Success ? 1 : 0);

            return ResultCode.Success;
        }

        [Command(26)] // 5.0.0+
        // AcquireBleServiceDiscoveryEvent() -> (byte<1>, handle<copy>)
        public ResultCode AcquireBleServiceDiscoveryEvent(ServiceCtx context)
        {
            KernelResult result = KernelResult.Success;

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(Os.GetReadableHandleOfSystemEvent(ref _bleServiceDiscoveryEvent));

            context.ResponseData.Write(result == KernelResult.Success ? 1 : 0);

            return ResultCode.Success;
        }

        [Command(33)] // 5.0.0+
        // AcquireBleMtuConfigEvent() -> (byte<1>, handle<copy>)
        public ResultCode AcquireBleMtuConfigEvent(ServiceCtx context)
        {
            KernelResult result = KernelResult.Success;

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(Os.GetReadableHandleOfSystemEvent(ref _bleMtuConfigEvent));

            context.ResponseData.Write(result == KernelResult.Success ? 1 : 0);

            return ResultCode.Success;
        }

        public void Dispose()
        {
            Os.DestroySystemEvent(ref _bleScanEvent);
            Os.DestroySystemEvent(ref _bleConnectionEvent);
            Os.DestroySystemEvent(ref _bleServiceDiscoveryEvent);
            Os.DestroySystemEvent(ref _bleMtuConfigEvent);
        }
    }
}