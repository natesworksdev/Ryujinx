namespace Ryujinx.HLE.HOS.Services.Time.Clock
{
    class EphemeralNetworkSystemClockCore : SystemClockCore
    {
        public EphemeralNetworkSystemClockCore(SteadyClockCore steadyClockCore) : base(steadyClockCore) { }

        protected override ResultCode Flush(SystemClockContext context)
        {
            return ResultCode.Success;
        }
    }
}
