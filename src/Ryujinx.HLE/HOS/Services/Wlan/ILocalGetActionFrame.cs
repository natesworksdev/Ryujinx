namespace Ryujinx.HLE.HOS.Services.Wlan
{
    [Service("wlan:lga")]
    sealed class ILocalGetActionFrame : IpcService
    {
        public ILocalGetActionFrame(ServiceCtx context) { }
    }
}