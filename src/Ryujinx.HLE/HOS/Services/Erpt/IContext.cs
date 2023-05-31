namespace Ryujinx.HLE.HOS.Services.Erpt
{
    [Service("erpt:c")]
    sealed class IContext : IpcService
    {
        public IContext(ServiceCtx context) { }
    }
}