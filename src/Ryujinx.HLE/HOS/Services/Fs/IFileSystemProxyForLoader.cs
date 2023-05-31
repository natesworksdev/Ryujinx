namespace Ryujinx.HLE.HOS.Services.Fs
{
    [Service("fsp-ldr")]
    sealed class IFileSystemProxyForLoader : IpcService
    {
        public IFileSystemProxyForLoader(ServiceCtx context) { }
    }
}