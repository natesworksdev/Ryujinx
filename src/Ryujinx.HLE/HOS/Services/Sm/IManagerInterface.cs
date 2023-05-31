namespace Ryujinx.HLE.HOS.Services.Sm
{
    [Service("sm:m")]
    sealed class IManagerInterface : IpcService
    {
        public IManagerInterface(ServiceCtx context) { }
    }
}