using Ryujinx.HLE.HOS.Services.Nifm.StaticService;

namespace Ryujinx.HLE.HOS.Services.Nifm
{
    [Service("nifm:a")] // Max sessions: 2
    [Service("nifm:s")] // Max sessions: 16
    [Service("nifm:u")] // Max sessions: 5
    class IStaticService : IpcService
    {
#pragma warning disable IDE0060
        public IStaticService(ServiceCtx context) { }
#pragma warning restore IDE0060

        [CommandCmif(4)]
        // CreateGeneralServiceOld() -> object<nn::nifm::detail::IGeneralService>
        public ResultCode CreateGeneralServiceOld(ServiceCtx context)
        {
            MakeObject(context, new IGeneralService());

            return ResultCode.Success;
        }

        [CommandCmif(5)] // 3.0.0+
        // CreateGeneralService(u64, pid) -> object<nn::nifm::detail::IGeneralService>
        public ResultCode CreateGeneralService(ServiceCtx context)
        {
            MakeObject(context, new IGeneralService());

            return ResultCode.Success;
        }
    }
}