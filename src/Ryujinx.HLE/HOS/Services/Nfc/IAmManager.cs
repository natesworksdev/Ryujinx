namespace Ryujinx.HLE.HOS.Services.Nfc
{
    [Service("nfc:am")]
    sealed class IAmManager : IpcService
    {
        public IAmManager(ServiceCtx context) { }
    }
}