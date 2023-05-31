namespace Ryujinx.HLE.HOS.Services.Account.Dauth
{
    [Service("dauth:0")] // 5.0.0+
    sealed class IService : IpcService
    {
        public IService(ServiceCtx context) { }
    }
}