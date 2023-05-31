namespace Ryujinx.HLE.HOS.Services.Hid
{
    [Service("xcd:sys")]
    sealed class ISystemServer : IpcService
    {
        public ISystemServer(ServiceCtx context) { }
    }
}