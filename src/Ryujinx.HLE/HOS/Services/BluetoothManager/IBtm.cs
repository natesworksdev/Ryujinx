namespace Ryujinx.HLE.HOS.Services.BluetoothManager
{
    [Service("btm")]
    sealed class IBtm : IpcService
    {
        public IBtm(ServiceCtx context) { }
    }
}