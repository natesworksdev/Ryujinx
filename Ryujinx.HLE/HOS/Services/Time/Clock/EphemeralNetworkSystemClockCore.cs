namespace Ryujinx.HLE.HOS.Services.Time.Clock
{
    class EphemeralNetworkSystemClockCore : SystemClockCore
    {
        private static EphemeralNetworkSystemClockCore instance;

        public static EphemeralNetworkSystemClockCore Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EphemeralNetworkSystemClockCore(TickBasedSteadyClockCore.Instance);
                }

                return instance;
            }
        }

        public EphemeralNetworkSystemClockCore(SteadyClockCore steadyClockCore) : base(steadyClockCore) { }

        public override ResultCode Flush(SystemClockContext context)
        {
            return ResultCode.Success;
        }
    }
}
