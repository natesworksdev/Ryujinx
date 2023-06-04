namespace Ryujinx.HLE.HOS.Services.Fatal
{
    [Service("fatal:p")]
    class IPrivateService : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public IPrivateService(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}