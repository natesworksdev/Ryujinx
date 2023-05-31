namespace Ryujinx.HLE.HOS.Services.Wlan
{
    [Service("wlan:sg")]
    sealed class ISocketGetFrame : IpcService
    {
        public ISocketGetFrame(ServiceCtx context) { }
    }
}