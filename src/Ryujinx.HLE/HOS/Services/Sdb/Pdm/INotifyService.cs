namespace Ryujinx.HLE.HOS.Services.Sdb.Pdm
{
    [Service("pdm:ntfy")]
    sealed class INotifyService : IpcService
    {
        public INotifyService(ServiceCtx context) { }
    }
}