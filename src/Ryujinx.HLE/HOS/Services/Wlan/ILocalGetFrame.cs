namespace Ryujinx.HLE.HOS.Services.Wlan
{
    [Service("wlan:lg")]
    sealed class ILocalGetFrame : IpcService
    {
        public ILocalGetFrame(ServiceCtx context) { }
    }
}