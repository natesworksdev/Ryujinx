namespace Ryujinx.HLE.HOS.Services.Usb
{
    [Service("usb:pd:c")]
    sealed class IPdCradleManager : IpcService
    {
        public IPdCradleManager(ServiceCtx context) { }
    }
}