using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Services.OsTypes;
using System;

namespace Ryujinx.HLE.HOS.Services.Ptm.Psm
{
    class IPsmSession : IpcService, IDisposable
    {
        private SystemEventType _stateChangeEvent;

        public IPsmSession(Horizon system)
        {
            Os.CreateSystemEvent(out _stateChangeEvent, EventClearMode.AutoClear, true);
        }

        [Command(0)]
        // BindStateChangeEvent() -> KObject
        public ResultCode BindStateChangeEvent(ServiceCtx context)
        {
            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(Os.GetReadableHandleOfSystemEvent(ref _stateChangeEvent));

            Logger.Stub?.PrintStub(LogClass.ServicePsm);

            return ResultCode.Success;
        }

        [Command(1)]
        // UnbindStateChangeEvent()
        public ResultCode UnbindStateChangeEvent(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServicePsm);

            return ResultCode.Success;
        }

        [Command(2)]
        // SetChargerTypeChangeEventEnabled(u8)
        public ResultCode SetChargerTypeChangeEventEnabled(ServiceCtx context)
        {
            bool chargerTypeChangeEventEnabled = context.RequestData.ReadBoolean();

            Logger.Stub?.PrintStub(LogClass.ServicePsm, new { chargerTypeChangeEventEnabled });

            return ResultCode.Success;
        }

        [Command(3)]
        // SetPowerSupplyChangeEventEnabled(u8)
        public ResultCode SetPowerSupplyChangeEventEnabled(ServiceCtx context)
        {
            bool powerSupplyChangeEventEnabled = context.RequestData.ReadBoolean();

            Logger.Stub?.PrintStub(LogClass.ServicePsm, new { powerSupplyChangeEventEnabled });

            return ResultCode.Success;
        }

        [Command(4)]
        // SetBatteryVoltageStateChangeEventEnabled(u8)
        public ResultCode SetBatteryVoltageStateChangeEventEnabled(ServiceCtx context)
        {
            bool batteryVoltageStateChangeEventEnabled = context.RequestData.ReadBoolean();

            Logger.Stub?.PrintStub(LogClass.ServicePsm, new { batteryVoltageStateChangeEventEnabled });

            return ResultCode.Success;
        }

        public void Dispose()
        {
            Os.DestroySystemEvent(ref _stateChangeEvent);
        }
    }
}