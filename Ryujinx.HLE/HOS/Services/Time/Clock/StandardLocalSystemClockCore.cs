namespace Ryujinx.HLE.HOS.Services.Time.Clock
{
    class StandardLocalSystemClockCore : SystemClockCore
    {

        private static StandardLocalSystemClockCore instance;

        public static StandardLocalSystemClockCore Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new StandardLocalSystemClockCore(StandardSteadyClockCore.Instance);
                }

                return instance;
            }
        }

        public StandardLocalSystemClockCore(StandardSteadyClockCore steadyClockCore) : base(steadyClockCore) {}

        public override ResultCode Flush(SystemClockContext context)
        {
            // TODO: set:sys SetUserSystemClockContext

            return ResultCode.Success;
        }
    }
}
