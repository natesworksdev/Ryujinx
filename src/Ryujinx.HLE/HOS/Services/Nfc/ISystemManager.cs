using Ryujinx.HLE.HOS.Services.Nfc.NfcManager;

namespace Ryujinx.HLE.HOS.Services.Nfc
{
    [Service("nfc:sys")]
    class ISystemManager : IpcService
    {
#pragma warning disable IDE0060
        public ISystemManager(ServiceCtx context) { }
#pragma warning restore IDE0060

        [CommandCmif(0)]
        // CreateSystemInterface() -> object<nn::nfc::detail::ISystem>
        public ResultCode CreateSystemInterface(ServiceCtx context)
        {
            MakeObject(context, new INfc(NfcPermissionLevel.System));

            return ResultCode.Success;
        }
    }
}
