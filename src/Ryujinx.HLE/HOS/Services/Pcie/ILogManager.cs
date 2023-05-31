namespace Ryujinx.HLE.HOS.Services.Pcie
{
    [Service("pcie:log")]
    sealed class ILogManager : IpcService
    {
        public ILogManager(ServiceCtx context) { }
    }
}