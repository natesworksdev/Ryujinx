namespace Ryujinx.HLE.HOS.Services.Wlan
{
    [Service("wlan:soc")]
    sealed class ISocketManager : IpcService
    {
        public ISocketManager(ServiceCtx context) { }
    }
}