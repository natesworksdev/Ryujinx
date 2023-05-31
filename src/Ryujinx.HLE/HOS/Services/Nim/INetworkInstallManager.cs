namespace Ryujinx.HLE.HOS.Services.Nim
{
    [Service("nim")]
    sealed class INetworkInstallManager : IpcService
    {
        public INetworkInstallManager(ServiceCtx context) { }
    }
}