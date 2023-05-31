namespace Ryujinx.HLE.HOS.Services.BluetoothManager
{
    [Service("btm:dbg")]
    sealed class IBtmDebug : IpcService
    {
        public IBtmDebug(ServiceCtx context) { }
    }
}