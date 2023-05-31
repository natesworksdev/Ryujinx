namespace Ryujinx.HLE.HOS.Services.Wlan
{
    [Service("wlan:lcl")]
    sealed class ILocalManager : IpcService
    {
        public ILocalManager(ServiceCtx context) { }
    }
}