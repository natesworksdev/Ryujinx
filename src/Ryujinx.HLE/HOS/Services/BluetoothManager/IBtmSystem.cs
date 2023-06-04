namespace Ryujinx.HLE.HOS.Services.BluetoothManager
{
    [Service("btm:sys")]
    class IBtmSystem : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public IBtmSystem(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}