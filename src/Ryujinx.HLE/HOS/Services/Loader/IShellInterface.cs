namespace Ryujinx.HLE.HOS.Services.Loader
{
    [Service("ldr:shel")]
    sealed class IShellInterface : IpcService
    {
        public IShellInterface(ServiceCtx context) { }
    }
}