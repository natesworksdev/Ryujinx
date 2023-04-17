using Ryujinx.HLE.HOS.Services.Nfc.Nfp.NfpManager;

namespace Ryujinx.HLE.HOS.Services.Nfc.Nfp
{
    [Service("nfp:dbg")]
    class IAmManager : IpcService
    {
#pragma warning disable IDE0060
        public IAmManager(ServiceCtx context) { }
#pragma warning restore IDE0060

        [CommandCmif(0)]
        // CreateDebugInterface() -> object<nn::nfp::detail::IDebug>
        public ResultCode CreateDebugInterface(ServiceCtx context)
        {
            MakeObject(context, new INfp(NfpPermissionLevel.Debug));

            return ResultCode.Success;
        }
    }
}