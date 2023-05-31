namespace Ryujinx.HLE.HOS.Services.Nfc.Mifare
{
    [Service("nfc:mf:u")]
    sealed class IUserManager : IpcService
    {
        public IUserManager(ServiceCtx context) { }
    }
}