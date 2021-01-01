namespace Ryujinx.HLE.HOS.Services.Apm
{
    [Service("apm:p")] // 8.0.0-
    class IManagerPrivileged : IManager
    {
        private readonly ServiceCtx _context;

        public IManagerPrivileged(ServiceCtx context) : base(context)
        {
            _context = context;
        }

        [Command(0)]
        protected override ResultCode OpenSession(out SessionServer sessionServer)
        {
            sessionServer = new SessionServer(_context);

            return ResultCode.Success;
        }

        protected override PerformanceMode GetPerformanceMode()
        {
            // not implemented
            return _context.Device.System.PerformanceState.PerformanceMode;
        }

        protected override bool IsCpuOverclockEnabled()
        {
            // not implemented
            return _context.Device.System.PerformanceState.CpuOverclockEnabled;
        }
    }
}