namespace Ryujinx.HLE.HOS.Services.Ns
{
    [Service("ns:su")]
    class ISystemUpdateInterface : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public ISystemUpdateInterface(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}