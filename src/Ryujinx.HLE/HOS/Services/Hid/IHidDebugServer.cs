namespace Ryujinx.HLE.HOS.Services.Hid
{
    [Service("hid:dbg")]
    class IHidDebugServer : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public IHidDebugServer(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}