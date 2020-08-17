using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Services.OsTypes;
using Ryujinx.HLE.HOS.Services.Settings;

namespace Ryujinx.HLE.HOS.Services.Bluetooth
{
    [Service("bt")]
    class IBluetoothUser : IpcService
    {
        public IBluetoothUser(ServiceCtx context) { }

        [Command(9)]
        // RegisterBleEvent(pid) -> handle<copy>
        public ResultCode RegisterBleEvent(ServiceCtx context)
        {
            NxSettings.Settings.TryGetValue("bluetooth_debug!skip_boot", out object debugMode);

            var bluetoothEventManager = context.Device.System.BluetoothEventManager;

            if ((bool)debugMode)
            {
                context.Response.HandleDesc = IpcHandleDesc.MakeCopy(Os.GetReadableHandleOfSystemEvent(ref bluetoothEventManager.RegisterBleDebugEvent));
            }
            else
            {
                context.Response.HandleDesc = IpcHandleDesc.MakeCopy(Os.GetReadableHandleOfSystemEvent(ref bluetoothEventManager.RegisterBleEvent));
            }

            return ResultCode.Success;
        }
    }
}