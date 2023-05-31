namespace Ryujinx.HLE.HOS.Services.Bgct
{
    [Service("bgtc:sc")]
    sealed class IStateControlService : IpcService
    {
        public IStateControlService(ServiceCtx context) { }
    }
}