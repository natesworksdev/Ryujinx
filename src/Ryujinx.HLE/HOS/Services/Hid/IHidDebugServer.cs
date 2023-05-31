namespace Ryujinx.HLE.HOS.Services.Hid
{
    [Service("hid:dbg")]
    sealed class IHidDebugServer : IpcService
    {
        public IHidDebugServer(ServiceCtx context) { }
    }
}