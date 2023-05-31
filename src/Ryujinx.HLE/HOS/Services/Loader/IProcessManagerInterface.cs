namespace Ryujinx.HLE.HOS.Services.Loader
{
    [Service("ldr:pm")]
    sealed class IProcessManagerInterface : IpcService
    {
        public IProcessManagerInterface(ServiceCtx context) { }
    }
}