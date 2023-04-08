namespace Ryujinx.HLE.HOS.Services.Account.Acc
{
    [Service("acc:aa", AccountServiceFlag.BaasAccessTokenAccessor)] // Max Sessions: 4
    class IBaasAccessTokenAccessor : IpcService
    {
#pragma warning disable IDE0060
        public IBaasAccessTokenAccessor(ServiceCtx context) { }
#pragma warning restore IDE0060
    }
}
