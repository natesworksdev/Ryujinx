namespace Ryujinx.HLE.HOS.Services.Apm
{
    [Service("apm")]
    [Service("apm:am")] // 8.0.0+
    class ManagerServer : IManager
    {
        public ManagerServer(ServiceCtx context) : base(context) { }

        protected override ResultCode OpenSession(out ISession sessionObj)
        {
            sessionObj = new ISession();

            return ResultCode.Success;
        }

        protected override PerformanceMode GetPerformanceMode()
        {
            return PerformanceState.PerformanceMode;
        }

        protected override bool IsCpuOverclockEnabled()
        {
            return PerformanceState.CpuOverclockEnabled;
        }
    }
}