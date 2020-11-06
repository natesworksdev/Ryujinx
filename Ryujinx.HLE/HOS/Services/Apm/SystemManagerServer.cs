namespace Ryujinx.HLE.HOS.Services.Apm
{
    [Service("apm:sys")]
    class SystemManagerServer : ISystemManager
    {
        public SystemManagerServer(ServiceCtx context) : base(context) { }

        protected override void RequestPerformanceMode(PerformanceMode performanceMode)
        {
            PerformanceState.PerformanceMode = performanceMode;
        }

        protected override void SetCpuBoostMode(CpuBoostMode cpuBoostMode)
        {
            PerformanceState.CpuBoostMode = cpuBoostMode;
        }

        protected override PerformanceConfiguration GetCurrentPerformanceConfiguration()
        {
            return PerformanceState.GetCurrentPerformanceConfiguration(PerformanceState.PerformanceMode);
        }
    }
}