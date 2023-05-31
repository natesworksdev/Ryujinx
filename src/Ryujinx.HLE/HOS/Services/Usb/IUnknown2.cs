namespace Ryujinx.HLE.HOS.Services.Usb
{
    [Service("usb:obsv")] // 8.0.0+
    sealed class IUnknown2 : IpcService
    {
        public IUnknown2(ServiceCtx context) { }
    }
}