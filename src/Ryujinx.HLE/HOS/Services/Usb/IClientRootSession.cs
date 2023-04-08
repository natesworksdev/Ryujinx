namespace Ryujinx.HLE.HOS.Services.Usb
{
    [Service("usb:hs")]
    [Service("usb:hs:a")] // 7.0.0+
    class IClientRootSession : IpcService
    {
#pragma warning disable IDE0060
        public IClientRootSession(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}
