using Ryujinx.HLE.HOS.Services.OsTypes;

namespace Ryujinx.HLE.HOS.Services.Bluetooth.BluetoothDriver
{
    class BluetoothEventManager
    {
        public SystemEventType InitializeBleDebugEvent;
        public SystemEventType UnknownBleDebugEvent;
        public SystemEventType RegisterBleDebugEvent;

        public SystemEventType InitializeBleEvent;
        public SystemEventType UnknownBleEvent;
        public SystemEventType RegisterBleEvent;
    }
}