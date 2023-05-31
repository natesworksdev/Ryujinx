namespace Ryujinx.HLE.HOS.Services.Erpt
{
    [Service("erpt:r")]
    sealed class ISession : IpcService
    {
        public ISession(ServiceCtx context) { }
    }
}