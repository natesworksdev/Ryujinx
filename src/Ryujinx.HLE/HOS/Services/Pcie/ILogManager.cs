namespace Ryujinx.HLE.HOS.Services.Pcie
{
    [Service("pcie:log")]
    class ILogManager : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public ILogManager(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}