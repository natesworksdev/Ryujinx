using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Nim.Ntc.StaticService;

namespace Ryujinx.HLE.HOS.Services.Nim.Ntc
{
    [Service("ntc")]
    class IStaticService : IpcService
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public IStaticService(ServiceCtx context) { }
#pragma warning restore IDE0060

        [CommandCmif(0)]
        // OpenEnsureNetworkClockAvailabilityService(u64) -> object<nn::ntc::detail::service::IEnsureNetworkClockAvailabilityService>
        public ResultCode CreateAsyncInterface(ServiceCtx context)
        {
            ulong unknown = context.RequestData.ReadUInt64();

            MakeObject(context, new IEnsureNetworkClockAvailabilityService(context));

            Logger.Stub?.PrintStub(LogClass.ServiceNtc, new { unknown });

            return ResultCode.Success;
        }
    }
}