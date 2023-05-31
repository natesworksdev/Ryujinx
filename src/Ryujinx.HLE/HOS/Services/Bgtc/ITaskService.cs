namespace Ryujinx.HLE.HOS.Services.Bgct
{
    [Service("bgtc:t")]
    sealed class ITaskService : IpcService
    {
        public ITaskService(ServiceCtx context) { }
    }
}