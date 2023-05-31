namespace Ryujinx.HLE.HOS.Services.Usb
{
    [Service("usb:pm")]
    sealed class IPmService : IpcService
    {
        public IPmService(ServiceCtx context) { }
    }
}