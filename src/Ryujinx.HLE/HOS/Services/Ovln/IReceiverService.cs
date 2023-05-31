namespace Ryujinx.HLE.HOS.Services.Ovln
{
    [Service("ovln:rcv")]
    sealed class IReceiverService : IpcService
    {
        public IReceiverService(ServiceCtx context) { }
    }
}