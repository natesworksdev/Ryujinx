using Ryujinx.Horizon.Sdk.OsTypes;
using Ryujinx.HLE.HOS.Services.Settings;

namespace Ryujinx.HLE.HOS.Services.Bluetooth
{
    [Service("btdrv")]
    class IBluetoothDriver : IpcService
    {
#pragma warning disable CS0414
        private string _unknownLowEnergy;
#pragma warning restore CS0414

        public IBluetoothDriver(ServiceCtx context) { }

        [Command(46)]
        // InitializeBluetoothLe() -> handle<copy>
        public ResultCode InitializeBluetoothLe(ServiceCtx context)
        {
            NxSettings.Settings.TryGetValue("bluetooth_debug!skip_boot", out object debugMode);

            var bluetoothEventManager = context.Device.System.BluetoothEventManager;

            if ((bool)debugMode)
            {
                Os.CreateSystemEvent(out bluetoothEventManager.InitializeBleDebugEvent, EventClearMode.AutoClear, true);
                Os.CreateSystemEvent(out bluetoothEventManager.UnknownBleDebugEvent, EventClearMode.AutoClear, true);
                Os.CreateSystemEvent(out bluetoothEventManager.RegisterBleDebugEvent, EventClearMode.AutoClear, true);
            }
            else
            {
                _unknownLowEnergy = "low_energy";

                Os.CreateSystemEvent(out bluetoothEventManager.InitializeBleEvent, EventClearMode.AutoClear, true);
                Os.CreateSystemEvent(out bluetoothEventManager.UnknownBleEvent, EventClearMode.AutoClear, true);
                Os.CreateSystemEvent(out bluetoothEventManager.RegisterBleEvent, EventClearMode.AutoClear, true);
            }

            return ResultCode.Success;
        }
    }
}