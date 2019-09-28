namespace Ryujinx.HLE.HOS.Services.Time.Clock
{
    class StandardLocalSystemClockCore : SystemClockCore
    {
        public StandardLocalSystemClockCore(StandardSteadyClockCore steadyClockCore) : base(steadyClockCore) {}

        protected override ResultCode Flush(SystemClockContext context)
        {
            // TODO: set:sys SetUserSystemClockContext

            return ResultCode.Success;
        }
    }
}
