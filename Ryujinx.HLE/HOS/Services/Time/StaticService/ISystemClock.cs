using Ryujinx.Common;
using Ryujinx.HLE.HOS.Services.Time.Clock;

namespace Ryujinx.HLE.HOS.Services.Time.StaticService
{
    class ISystemClock : IpcService
    {
        private SystemClockCore _clockCore;
        private bool            _writePermission;
        private bool           _bypassUninitializedClock;

        public ISystemClock(SystemClockCore clockCore, bool writePermission, bool bypassUninitializedCloc)
        {
            _clockCore                = clockCore;
            _writePermission          = writePermission;
            _bypassUninitializedClock = bypassUninitializedCloc;
        }

        [Command(0)]
        // GetCurrentTime() -> nn::time::PosixTime
        public ResultCode GetCurrentTime(ServiceCtx context)
        {
            if (!_bypassUninitializedClock && !_clockCore.IsInitialized())
            {
                return ResultCode.UninitializedClock;
            }

            ResultCode result = _clockCore.GetCurrentTime(context.Thread, out long posixTime);

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

            return _clockCore.SetCurrentTime(context.Thread, posixTime);
        }

        [Command(2)]
        // GetClockContext() -> nn::time::SystemClockContext
        public ResultCode GetSystemClockContext(ServiceCtx context)
        {
            if (!_bypassUninitializedClock && !_clockCore.IsInitialized())
            {
                return ResultCode.UninitializedClock;
            }

            ResultCode result = _clockCore.GetClockContext(context.Thread, out SystemClockContext clockContext);

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
    }
}