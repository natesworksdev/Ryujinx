namespace Ryujinx.HLE.HOS.Services.Account.Acc
{
    [Service("acc:aa", AccountServiceFlag.BaasAccessTokenAccessor)] // Max Sessions: 4
    sealed class IBaasAccessTokenAccessor : IpcService
    {
        public IBaasAccessTokenAccessor(ServiceCtx context) { }
    }
}