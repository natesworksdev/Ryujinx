namespace Ryujinx.HLE.HOS.Services.Fs
{
    [Service("fsp-ldr")]
    class IFileSystemProxyForLoader : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public IFileSystemProxyForLoader(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}