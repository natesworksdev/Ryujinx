using Ryujinx.Common;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Services.OsTypes;
using Ryujinx.HLE.HOS.Services.Time.Clock;
using Ryujinx.Horizon.Kernel;
using System;

namespace Ryujinx.HLE.HOS.Services.Time.StaticService
{
    class ISystemClock : IpcService, IDisposable
    {
        private SystemClockCore _clockCore;
        private bool            _writePermission;
        private bool            _bypassUninitializedClock;
        private SystemEventType _operationEvent;
        private bool            _operationEventRegistered;
        public ISystemClock(SystemClockCore clockCore, bool writePermission, bool bypassUninitializedClock)
        {
            _clockCore                    = clockCore;
            _writePermission              = writePermission;
            _bypassUninitializedClock     = bypassUninitializedClock;

            Os.CreateSystemEvent(out _operationEvent, EventClearMode.AutoClear, true);
        }

        [Command(0)]
        // GetCurrentTime() -> nn::time::PosixTime
        public ResultCode GetCurrentTime(ServiceCtx context)
        {
            if (!_bypassUninitializedClock && !_clockCore.IsInitialized())
            {
                return ResultCode.UninitializedClock;
            }

            ResultCode result = _clockCore.GetCurrentTime(out long posixTime);

            if (result == ResultCode.Success)
            {
                context.ResponseData.Write(posixTime);
            }

            return result;
        }

        [Command(1)]
        // SetCurrentTime(nn::time::PosixTime)
        public ResultCode SetCurrentTime(ServiceCtx context)
        {
            if (!_writePermission)
            {
                return ResultCode.PermissionDenied;
            }

            if (!_bypassUninitializedClock && !_clockCore.IsInitialized())
            {
                return ResultCode.UninitializedClock;
            }

            long posixTime = context.RequestData.ReadInt64();

            return _clockCore.SetCurrentTime(posixTime);
        }

        [Command(2)]
        // GetClockContext() -> nn::time::SystemClockContext
        public ResultCode GetSystemClockContext(ServiceCtx context)
        {
            if (!_bypassUninitializedClock && !_clockCore.IsInitialized())
            {
                return ResultCode.UninitializedClock;
            }

            ResultCode result = _clockCore.GetClockContext(out SystemClockContext clockContext);

            if (result == ResultCode.Success)
            {
                context.ResponseData.WriteStruct(clockContext);
            }

            return result;
        }

        [Command(3)]
        // SetClockContext(nn::time::SystemClockContext)
        public ResultCode SetSystemClockContext(ServiceCtx context)
        {
            if (!_writePermission)
            {
                return ResultCode.PermissionDenied;
            }

            if (!_bypassUninitializedClock && !_clockCore.IsInitialized())
            {
                return ResultCode.UninitializedClock;
            }

            SystemClockContext clockContext = context.RequestData.ReadStruct<SystemClockContext>();

            ResultCode result = _clockCore.SetSystemClockContext(clockContext);

            return result;
        }

        [Command(4)] // 9.0.0+
        // GetOperationEventReadableHandle() -> handle<copy>
        public ResultCode GetOperationEventReadableHandle(ServiceCtx context)
        {
            if (!_operationEventRegistered)
            {
                _operationEventRegistered = true;

                _clockCore.RegisterOperationEvent(KernelStatic.GetSignalableEvent(Os.GetWritableHandleOfSystemEvent(ref _operationEvent)));
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(Os.GetReadableHandleOfSystemEvent(ref _operationEvent));

            return ResultCode.Success;
        }

        public void Dispose()
        {
            Os.DestroySystemEvent(ref _operationEvent);
        }
    }
}