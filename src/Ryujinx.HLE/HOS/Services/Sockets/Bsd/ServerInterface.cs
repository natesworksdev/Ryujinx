namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd
{
    [Service("bsdcfg")]
    sealed class ServerInterface : IpcService
    {
        public ServerInterface(ServiceCtx context) { }
    }
}