namespace Ryujinx.HLE.HOS.Services.Time.Clock
{
    sealed class EphemeralNetworkSystemClockCore : SystemClockCore
    {
        public EphemeralNetworkSystemClockCore(SteadyClockCore steadyClockCore) : base(steadyClockCore) { }
    }
}
