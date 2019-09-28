using Ryujinx.HLE.HOS.Services.Time.Clock;
using Ryujinx.HLE.HOS.Services.Time.TimeZone;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.HLE.HOS.Services.Time
{
    class TimeManager
    {
        private static TimeManager _instance;

        public static TimeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TimeManager();
                }

                return _instance;
            }
        }

        public StandardSteadyClockCore         StandardSteadyClock         { get; private set; }
        public TickBasedSteadyClockCore        TickBasedSteadyClock        { get; private set; }
        public StandardLocalSystemClockCore    StandardLocalSystemClock    { get; private set; }
        public StandardNetworkSystemClockCore  StandardNetworkSystemClock  { get; private set; }
        public StandardUserSystemClockCore     StandardUserSystemClock     { get; private set; }
        public TimeZoneManager                 TimeZone                    { get; private set; }
        public EphemeralNetworkSystemClockCore EphemeralNetworkSystemClock { get; private set; }

        // TODO: 9.0.0+ power state, alarms, clock writers

        public TimeManager()
        {
            StandardSteadyClock         = new StandardSteadyClockCore();
            TickBasedSteadyClock        = new TickBasedSteadyClockCore();
            StandardLocalSystemClock    = new StandardLocalSystemClockCore(StandardSteadyClock);
            StandardNetworkSystemClock  = new StandardNetworkSystemClockCore(StandardSteadyClock);
            StandardUserSystemClock     = new StandardUserSystemClockCore(StandardLocalSystemClock, StandardNetworkSystemClock);
            TimeZone                    = new TimeZoneManager();
            EphemeralNetworkSystemClock = new EphemeralNetworkSystemClockCore(StandardSteadyClock);
        }
    }
}
