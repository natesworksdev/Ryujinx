namespace Ryujinx.HLE.HOS.Services.Time.Clock
{
    sealed class StandardLocalSystemClockCore : SystemClockCore
    {
        public StandardLocalSystemClockCore(StandardSteadyClockCore steadyClockCore) : base(steadyClockCore) {}
    }
}
