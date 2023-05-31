namespace Ryujinx.HLE.HOS.Services.Usb
{
    [Service("usb:ds")]
    sealed class IDsService : IpcService
    {
        public IDsService(ServiceCtx context) { }
    }
}