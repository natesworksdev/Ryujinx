namespace Ryujinx.HLE.HOS.Services.Grc
{
    [Service("grc:d")] // 6.0.0+
    sealed class IRemoteVideoTransfer : IpcService
    {
        public IRemoteVideoTransfer(ServiceCtx context) { }
    }
}