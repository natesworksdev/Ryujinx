namespace Ryujinx.HLE.HOS.Services.Fs
{
    [Service("fsp-pr")]
    sealed class IProgramRegistry : IpcService
    {
        public IProgramRegistry(ServiceCtx context) { }
    }
}