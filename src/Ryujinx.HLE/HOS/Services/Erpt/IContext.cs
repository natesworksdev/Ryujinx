namespace Ryujinx.HLE.HOS.Services.Erpt
{
    [Service("erpt:c")]
    class IContext : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public IContext(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}