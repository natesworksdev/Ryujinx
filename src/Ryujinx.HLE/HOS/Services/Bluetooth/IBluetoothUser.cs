using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Services.Bluetooth.BluetoothDriver;
using Ryujinx.HLE.HOS.Services.Settings;

namespace Ryujinx.HLE.HOS.Services.Bluetooth
{
    [Service("bt")]
    class IBluetoothUser : IpcService
    {
#pragma warning disable IDE0060
        public IBluetoothUser(ServiceCtx context) { }
#pragma warning restore IDE0060

        [CommandCmif(9)]
        // RegisterBleEvent(pid) -> handle<copy>
        public static ResultCode RegisterBleEvent(ServiceCtx context)
        {
            NxSettings.Settings.TryGetValue("bluetooth_debug!skip_boot", out object debugMode);

            if ((bool)debugMode)
            {
                context.Response.HandleDesc = IpcHandleDesc.MakeCopy(BluetoothEventManager.RegisterBleDebugEventHandle);
            }
            else
            {
                context.Response.HandleDesc = IpcHandleDesc.MakeCopy(BluetoothEventManager.RegisterBleEventHandle);
            }

            return ResultCode.Success;
        }
    }
}
