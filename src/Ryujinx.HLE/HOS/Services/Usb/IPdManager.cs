namespace Ryujinx.HLE.HOS.Services.Usb
{
    [Service("usb:pd")]
    sealed class IPdManager : IpcService
    {
        public IPdManager(ServiceCtx context) { }
    }
}