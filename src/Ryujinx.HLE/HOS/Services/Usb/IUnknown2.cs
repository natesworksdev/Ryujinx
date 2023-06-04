namespace Ryujinx.HLE.HOS.Services.Usb
{
    [Service("usb:obsv")] // 8.0.0+
    class IUnknown2 : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public IUnknown2(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}