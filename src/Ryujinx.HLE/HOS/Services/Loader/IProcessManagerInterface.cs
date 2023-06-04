namespace Ryujinx.HLE.HOS.Services.Loader
{
    [Service("ldr:pm")]
    class IProcessManagerInterface : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public IProcessManagerInterface(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}