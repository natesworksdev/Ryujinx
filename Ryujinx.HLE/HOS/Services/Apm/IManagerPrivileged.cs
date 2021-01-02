namespace Ryujinx.HLE.HOS.Services.Apm
{
    [Service("apm:p")] // 1.0.0-7.0.1
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
            return PerformanceMode.Default;
        }

        protected override bool IsCpuOverclockEnabled()
        {
            return false;
        }
    }
}