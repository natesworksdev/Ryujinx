namespace Ryujinx.HLE.HOS.Services.Pcie
{
    [Service("pcie")]
    sealed class IManager : IpcService
    {
        public IManager(ServiceCtx context) { }
    }
}