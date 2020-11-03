namespace Ryujinx.HLE.HOS.Services.Apm
{
    [Service("apm")]
    [Service("apm:am")] // 8.0.0+
    class IManager : IpcService
    {
        public IManager(ServiceCtx context) { }

        [Command(0)]
        // OpenSession() -> object<nn::apm::ISession>
        public ResultCode OpenSession(ServiceCtx context)
        {
            MakeObject(context, new ISession());

            return ResultCode.Success;
        }

        [Command(1)]
        // GetPerformanceMode() -> nn::apm::PerformanceMode
        public ResultCode GetPerformanceMode(ServiceCtx context)
        {
            return GetPerformanceModeImpl(context);
        }

        public static ResultCode GetPerformanceModeImpl(ServiceCtx context)
        {
            context.ResponseData.Write((uint)PerformanceState.PerformanceMode);

            return ResultCode.Success;
        }

        [Command(6)] // 7.0.0+
        // IsCpuOverclockEnabled() -> bool
        public ResultCode IsCpuOverclockEnabled(ServiceCtx context)
        {
            context.ResponseData.Write(PerformanceState.CpuOverclockEnabled);

            return ResultCode.Success;
        }
    }
}