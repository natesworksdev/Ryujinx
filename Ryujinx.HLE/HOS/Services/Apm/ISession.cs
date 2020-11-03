using Ryujinx.Common.Logging;

namespace Ryujinx.HLE.HOS.Services.Apm
{
    class ISession : IpcService
    {
        public ISession() { }

        [Command(0)]
        // SetPerformanceConfiguration(nn::apm::PerformanceMode, nn::apm::PerformanceConfiguration)
        public ResultCode SetPerformanceConfiguration(ServiceCtx context)
        {
            PerformanceMode          performanceMode          = (PerformanceMode)context.RequestData.ReadInt32();
            PerformanceConfiguration performanceConfiguration = (PerformanceConfiguration)context.RequestData.ReadInt32();

            if (performanceMode > PerformanceMode.Boost)
            {
                return ResultCode.InvalidParameters;
            }

            switch (performanceMode)
            {
                case PerformanceMode.Default: 
                    PerformanceState.DefaultPerformanceConfiguration = performanceConfiguration; 
                    break;
                case PerformanceMode.Boost:   
                    PerformanceState.BoostPerformanceConfiguration = performanceConfiguration; 
                    break;
                default:
                    Logger.Error?.PrintMsg(LogClass.ServiceApm, $"PerformanceMode isn't supported: {performanceMode}");
                    break;
            }

            return ResultCode.Success;
        }

        [Command(1)]
        // GetPerformanceConfiguration(nn::apm::PerformanceMode) -> nn::apm::PerformanceConfiguration
        public ResultCode GetPerformanceConfiguration(ServiceCtx context)
        {
            PerformanceMode performanceMode = (PerformanceMode)context.RequestData.ReadInt32();

            if (performanceMode > PerformanceMode.Boost)
            {
                return ResultCode.InvalidParameters;
            }

            context.ResponseData.Write((uint)PerformanceState.GetCurrentPerformanceConfiguration(performanceMode));

            return ResultCode.Success;
        }

        [Command(2)] // 8.0.0+
        // SetCpuOverclockEnabled(bool)
        public ResultCode SetCpuOverclockEnabled(ServiceCtx context)
        {
            PerformanceState.CpuOverclockEnabled = context.RequestData.ReadBoolean();

            // NOTE: This call seems to overclock the system, since we emulate it, it's fine to do nothing instead.

            return ResultCode.Success;
        }
    }
}