namespace Ryujinx.HLE.HOS.Services.Apm
{
    [Service("apm:sys")]
    class ISystemManager : IpcService
    {
        public ISystemManager(ServiceCtx context) { }

        [Command(0)]
        // RequestPerformanceMode(nn::apm::PerformanceMode)
        public ResultCode RequestPerformanceMode(ServiceCtx context)
        {
            PerformanceState.PerformanceMode = (PerformanceMode)context.RequestData.ReadInt32();

            // NOTE: This call seems to overclock the system related to the PerformanceMode, since we emulate it, it's fine to do nothing instead.

            return ResultCode.Success;
        }

        [Command(6)] // 7.0.0+
        // SetCpuBoostMode(nn::apm::CpuBootMode)
        public ResultCode SetCpuBoostMode(ServiceCtx context)
        {
            return SetCpuBoostModeImpl(context.RequestData.ReadUInt32());
        }

        public static ResultCode SetCpuBoostModeImpl(uint cpuBoostMode)
        {
            PerformanceState.CpuBoostMode = (CpuBoostMode)cpuBoostMode;

            // NOTE: This call seems to overclock the system related to the CpuBoostMode, since we emulate it, it's fine to do nothing instead.

            return ResultCode.Success;
        }

        [Command(7)] // 7.0.0+
        // GetCurrentPerformanceConfiguration() -> nn::apm::PerformanceConfiguration
        public ResultCode GetCurrentPerformanceConfiguration(ServiceCtx context)
        {
            return GetCurrentPerformanceConfigurationImpl(context);
        }

        public static ResultCode GetCurrentPerformanceConfigurationImpl(ServiceCtx context)
        {
            context.ResponseData.Write((uint)PerformanceState.GetCurrentPerformanceConfiguration(PerformanceState.PerformanceMode));

            return ResultCode.Success;
        }
    }
}