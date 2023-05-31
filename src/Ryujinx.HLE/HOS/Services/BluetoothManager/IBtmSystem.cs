namespace Ryujinx.HLE.HOS.Services.BluetoothManager
{
    [Service("btm:sys")]
    sealed class IBtmSystem : IpcService
    {
        public IBtmSystem(ServiceCtx context) { }
    }
}