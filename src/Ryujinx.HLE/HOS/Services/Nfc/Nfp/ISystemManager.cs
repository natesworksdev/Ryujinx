using Ryujinx.HLE.HOS.Services.Nfc.Nfp.NfpManager;

namespace Ryujinx.HLE.HOS.Services.Nfc.Nfp
{
    [Service("nfp:sys")]
    class ISystemManager : IpcService
    {
#pragma warning disable IDE0060
        public ISystemManager(ServiceCtx context) { }
#pragma warning restore IDE0060

        [CommandCmif(0)]
        // CreateSystemInterface() -> object<nn::nfp::detail::ISystem>
        public ResultCode CreateSystemInterface(ServiceCtx context)
        {
            MakeObject(context, new INfp(NfpPermissionLevel.System));

            return ResultCode.Success;
        }
    }
}