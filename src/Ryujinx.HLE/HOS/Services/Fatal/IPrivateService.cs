namespace Ryujinx.HLE.HOS.Services.Fatal
{
    [Service("fatal:p")]
    sealed class IPrivateService : IpcService
    {
        public IPrivateService(ServiceCtx context) { }
    }
}