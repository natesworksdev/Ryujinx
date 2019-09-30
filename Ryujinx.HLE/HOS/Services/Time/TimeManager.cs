using System;
using System.IO;
using Ryujinx.HLE.HOS.Kernel.Memory;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.Time.Clock;
using Ryujinx.HLE.HOS.Services.Time.TimeZone;
using Ryujinx.HLE.Utilities;

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

        public StandardSteadyClockCore                  StandardSteadyClock         { get; private set; }
        public TickBasedSteadyClockCore                 TickBasedSteadyClock        { get; private set; }
        public StandardLocalSystemClockCore             StandardLocalSystemClock    { get; private set; }
        public StandardNetworkSystemClockCore           StandardNetworkSystemClock  { get; private set; }
        public StandardUserSystemClockCore              StandardUserSystemClock     { get; private set; }
        public TimeZoneContentManager                   TimeZone                    { get; private set; }
        public EphemeralNetworkSystemClockCore          EphemeralNetworkSystemClock { get; private set; }
        public TimeSharedMemory                         SharedMemory                { get; private set; }
        // TODO: 9.0.0+ power states and alarms
        public LocalSystemClockContextWriter            LocalClockContextWriter     { get; private set; }
        public NetworkSystemClockContextWriter          NetworkClockContextWriter   { get; private set; }
        public EphemeralNetworkSystemClockContextWriter EphemeralClockContextWriter { get; private set; }

        public TimeManager()
        {
            StandardSteadyClock         = new StandardSteadyClockCore();
            TickBasedSteadyClock        = new TickBasedSteadyClockCore();
            StandardLocalSystemClock    = new StandardLocalSystemClockCore(StandardSteadyClock);
            StandardNetworkSystemClock  = new StandardNetworkSystemClockCore(StandardSteadyClock);
            StandardUserSystemClock     = new StandardUserSystemClockCore(StandardLocalSystemClock, StandardNetworkSystemClock);
            TimeZone                    = new TimeZoneContentManager();
            EphemeralNetworkSystemClock = new EphemeralNetworkSystemClockCore(StandardSteadyClock);
            SharedMemory                = new TimeSharedMemory();
            LocalClockContextWriter     = new LocalSystemClockContextWriter(SharedMemory);
            NetworkClockContextWriter   = new NetworkSystemClockContextWriter(SharedMemory);
            EphemeralClockContextWriter = new EphemeralNetworkSystemClockContextWriter();
        }

        public void Initialize(Switch device, Horizon system, KSharedMemory sharedMemory, long timeSharedMemoryAddress, int timeSharedMemorySize)
        {
            SharedMemory.Initialize(device, sharedMemory, timeSharedMemoryAddress, timeSharedMemorySize);

            // Here we use system on purpose as device.System isn't initialized at this point.
            StandardUserSystemClock.CreateAutomaticCorrectionEvent(system);
        }

        public void InitializeTimeZone(Switch device)
        {
            TimeZone.Initialize(this, device);
        }


        public ResultCode SetupStandardSteadyClock(KThread thread, UInt128 clockSourceId, TimeSpanType setupValue, TimeSpanType internalOffset, TimeSpanType testOffset, bool isRtcResetDetected)
        {
            SetupInternalStandardSteadyClock(clockSourceId, setupValue, internalOffset, testOffset, isRtcResetDetected);

            TimeSpanType currentTimePoint = StandardSteadyClock.GetCurrentRawTimePoint(thread);

            SharedMemory.SetupStandardSteadyClock(thread, clockSourceId, currentTimePoint);

            // TODO: propagate IPC late binding of "time:s" and "time:p"
            return ResultCode.Success;
        }

        private void SetupInternalStandardSteadyClock(UInt128 clockSourceId, TimeSpanType setupValue, TimeSpanType internalOffset, TimeSpanType testOffset, bool isRtcResetDetected)
        {
            StandardSteadyClock.SetClockSourceId(clockSourceId);
            StandardSteadyClock.SetSetupValue(setupValue);
            StandardSteadyClock.SetInternalOffset(internalOffset);
            StandardSteadyClock.SetTestOffset(testOffset);

            if (isRtcResetDetected)
            {
                StandardSteadyClock.SetRtcReset();
            }

            StandardSteadyClock.MarkInitialized();

            // TODO: propagate IPC late binding of "time:s" and "time:p"
        }

        public void SetupStandardLocalSystemClock(KThread thread, SystemClockContext clockContext, long posixTime)
        {
            StandardLocalSystemClock.SetUpdateCallbackInstance(LocalClockContextWriter);

            SteadyClockTimePoint currentTimePoint = StandardLocalSystemClock.GetSteadyClockCore().GetCurrentTimePoint(thread);
            if (currentTimePoint.ClockSourceId == clockContext.SteadyTimePoint.ClockSourceId)
            {
                StandardLocalSystemClock.SetSystemClockContext(clockContext);
            }
            else
            {
                // TODO: if the result of this is wrong, abort
                StandardLocalSystemClock.SetCurrentTime(thread, posixTime);
            }

            StandardLocalSystemClock.MarkInitialized();

            // TODO: propagate IPC late binding of "time:s" and "time:p"

        }

        public void SetupStandardNetworkSystemClock(SystemClockContext clockContext, TimeSpanType sufficientAccuracy)
        {
            StandardNetworkSystemClock.SetUpdateCallbackInstance(NetworkClockContextWriter);

            // TODO: if the result of this is wrong, abort
            StandardNetworkSystemClock.SetSystemClockContext(clockContext);

            StandardNetworkSystemClock.SetStandardNetworkClockSufficientAccuracy(sufficientAccuracy);
            StandardNetworkSystemClock.MarkInitialized();

            // TODO: propagate IPC late binding of "time:s" and "time:p"

        }

        public void SetupTimeZoneManager(string locationName, SteadyClockTimePoint timeZoneUpdatedTimePoint, uint totalLocationNameCount, UInt128 timeZoneRuleVersion, Stream timeZoneBinaryStream)
        {
            // TODO: if the result of this is wrong, abort
            TimeZone.Manager.SetDeviceLocationNameWithTimeZoneRule(locationName, timeZoneBinaryStream);

            TimeZone.Manager.SetUpdatedTime(timeZoneUpdatedTimePoint, true);
            TimeZone.Manager.SetTotalLocationNameCount(totalLocationNameCount);
            TimeZone.Manager.SetTimeZoneRuleVersion(timeZoneRuleVersion);
            TimeZone.Manager.MarkInitialized();

            // TODO: propagate IPC late binding of "time:s" and "time:p"

        }

        public void SetupEphemeralNetworkSystemClock()
        {
            EphemeralNetworkSystemClock.SetUpdateCallbackInstance(EphemeralClockContextWriter);
            EphemeralNetworkSystemClock.MarkInitialized();

            // TODO: propagate IPC late binding of "time:s" and "time:p"

        }

        public void SetupStandardUserSystemClock(KThread thread, bool isAutomaticCorrectionEnabled, SteadyClockTimePoint steadyClockTimePoint)
        {
            // TODO: if the result of this is wrong, abort
            StandardUserSystemClock.SetAutomaticCorrectionEnabled(thread, isAutomaticCorrectionEnabled);

            StandardUserSystemClock.SetAutomaticCorrectionUpdatedTime(steadyClockTimePoint);
            StandardUserSystemClock.MarkInitialized();

            SharedMemory.SetAutomaticCorrectionEnabled(isAutomaticCorrectionEnabled);

            // TODO: propagate IPC late binding of "time:s" and "time:p"

        }

        public void SetStandardSteadyClockRtcOffset(KThread thread, TimeSpanType rtcOffset)
        {
            StandardSteadyClock.SetSetupValue(rtcOffset);

            TimeSpanType currentTimePoint = StandardSteadyClock.GetCurrentRawTimePoint(thread);

            SharedMemory.SetSteadyClockRawTimePoint(thread, currentTimePoint);
        }
    }
}
