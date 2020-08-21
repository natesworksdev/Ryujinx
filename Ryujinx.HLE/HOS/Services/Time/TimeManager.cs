using Ryujinx.Configuration;
using Ryujinx.HLE.Exceptions;
using Ryujinx.HLE.HOS.Services.Pcv.Bpc;
using Ryujinx.HLE.HOS.Services.Settings;
using Ryujinx.HLE.HOS.Services.Time.Clock;
using Ryujinx.HLE.HOS.Services.Time.TimeZone;
using Ryujinx.HLE.Utilities;
using System;
using System.IO;

namespace Ryujinx.HLE.HOS.Services.Time
{
    class TimeManager : ServerBase
    {
        public StandardSteadyClockCore                  StandardSteadyClock         { get; }
        public TickBasedSteadyClockCore                 TickBasedSteadyClock        { get; }
        public StandardLocalSystemClockCore             StandardLocalSystemClock    { get; }
        public StandardNetworkSystemClockCore           StandardNetworkSystemClock  { get; }
        public StandardUserSystemClockCore              StandardUserSystemClock     { get; }
        public TimeZoneContentManager                   TimeZone                    { get; }
        public EphemeralNetworkSystemClockCore          EphemeralNetworkSystemClock { get; }
        public TimeSharedMemory                         SharedMemory                { get; }
        public LocalSystemClockContextWriter            LocalClockContextWriter     { get; }
        public NetworkSystemClockContextWriter          NetworkClockContextWriter   { get; }
        public EphemeralNetworkSystemClockContextWriter EphemeralClockContextWriter { get; }

        // TODO: 9.0.0+ power states and alarms

        public TimeManager(Switch device) : base(device.System.KernelContext, "TimeServer")
        {
            StandardSteadyClock         = new StandardSteadyClockCore();
            TickBasedSteadyClock        = new TickBasedSteadyClockCore();
            StandardLocalSystemClock    = new StandardLocalSystemClockCore(StandardSteadyClock);
            StandardNetworkSystemClock  = new StandardNetworkSystemClockCore(StandardSteadyClock);
            StandardUserSystemClock     = new StandardUserSystemClockCore(StandardLocalSystemClock, StandardNetworkSystemClock);
            TimeZone                    = new TimeZoneContentManager();
            EphemeralNetworkSystemClock = new EphemeralNetworkSystemClockCore(TickBasedSteadyClock);
            SharedMemory                = new TimeSharedMemory();
            LocalClockContextWriter     = new LocalSystemClockContextWriter(SharedMemory);
            NetworkClockContextWriter   = new NetworkSystemClockContextWriter(SharedMemory);
            EphemeralClockContextWriter = new EphemeralNetworkSystemClockContextWriter();
        }

        protected override void Initialize()
        {
            SharedMemory.Initialize();
            StandardUserSystemClock.CreateAutomaticCorrectionEvent();

            // TODO: use set:sys (and get external clock source id from settings)
            // TODO: use "time!standard_steady_clock_rtc_update_interval_minutes" and implement a worker thread to be accurate.
            UInt128 clockSourceId = new UInt128(Guid.NewGuid().ToByteArray());
            IRtcManager.GetExternalRtcValue(out ulong rtcValue);

            // We assume the rtc is system time.
            TimeSpanType systemTime = TimeSpanType.FromSeconds((long)rtcValue);

            // Configure and setup internal offset
            TimeSpanType internalOffset = TimeSpanType.FromSeconds(ConfigurationState.Instance.System.SystemTimeOffset);

            TimeSpanType systemTimeOffset = new TimeSpanType(systemTime.NanoSeconds + internalOffset.NanoSeconds);

            if (systemTime.IsDaylightSavingTime() && !systemTimeOffset.IsDaylightSavingTime())
            {
                internalOffset = internalOffset.AddSeconds(3600L);
            }
            else if (!systemTime.IsDaylightSavingTime() && systemTimeOffset.IsDaylightSavingTime())
            {
                internalOffset = internalOffset.AddSeconds(-3600L);
            }

            internalOffset = new TimeSpanType(-internalOffset.NanoSeconds);

            // First init the standard steady clock
            SetupStandardSteadyClock(clockSourceId, systemTime, internalOffset, TimeSpanType.Zero, false);
            SetupStandardLocalSystemClock(new SystemClockContext(), systemTime.ToSeconds());

            if (NxSettings.Settings.TryGetValue("time!standard_network_clock_sufficient_accuracy_minutes", out object standardNetworkClockSufficientAccuracyMinutes))
            {
                TimeSpanType standardNetworkClockSufficientAccuracy = new TimeSpanType((int)standardNetworkClockSufficientAccuracyMinutes * 60000000000);

                // The network system clock needs a valid system clock, as such we setup this system clock using the local system clock.
                StandardLocalSystemClock.GetClockContext(out SystemClockContext localSytemClockContext);
                SetupStandardNetworkSystemClock(localSytemClockContext, standardNetworkClockSufficientAccuracy);
            }

            SetupStandardUserSystemClock(false, SteadyClockTimePoint.GetRandom());

            // FIXME: TimeZone shoud be init here but it's actually done in ContentManager

            SetupEphemeralNetworkSystemClock();
        }

        public void InitializeTimeZone(Switch device)
        {
            TimeZone.Initialize(this, device);
        }

        public void SetupStandardSteadyClock(UInt128 clockSourceId, TimeSpanType setupValue, TimeSpanType internalOffset, TimeSpanType testOffset, bool isRtcResetDetected)
        {
            SetupInternalStandardSteadyClock(clockSourceId, setupValue, internalOffset, testOffset, isRtcResetDetected);

            TimeSpanType currentTimePoint = StandardSteadyClock.GetCurrentRawTimePoint();

            SharedMemory.SetupStandardSteadyClock(clockSourceId, currentTimePoint);

            // TODO: propagate IPC late binding of "time:s" and "time:p"
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

        public void SetupStandardLocalSystemClock(SystemClockContext clockContext, long posixTime)
        {
            StandardLocalSystemClock.SetUpdateCallbackInstance(LocalClockContextWriter);

            SteadyClockTimePoint currentTimePoint = StandardLocalSystemClock.GetSteadyClockCore().GetCurrentTimePoint();
            if (currentTimePoint.ClockSourceId == clockContext.SteadyTimePoint.ClockSourceId)
            {
                StandardLocalSystemClock.SetSystemClockContext(clockContext);
            }
            else
            {
                if (StandardLocalSystemClock.SetCurrentTime(posixTime) != ResultCode.Success)
                {
                    throw new InternalServiceException("Cannot set current local time");
                }
            }

            StandardLocalSystemClock.MarkInitialized();

            // TODO: propagate IPC late binding of "time:s" and "time:p"
        }

        public void SetupStandardNetworkSystemClock(SystemClockContext clockContext, TimeSpanType sufficientAccuracy)
        {
            StandardNetworkSystemClock.SetUpdateCallbackInstance(NetworkClockContextWriter);

            if (StandardNetworkSystemClock.SetSystemClockContext(clockContext) != ResultCode.Success)
            {
                throw new InternalServiceException("Cannot set network SystemClockContext");
            }

            StandardNetworkSystemClock.SetStandardNetworkClockSufficientAccuracy(sufficientAccuracy);
            StandardNetworkSystemClock.MarkInitialized();

            // TODO: propagate IPC late binding of "time:s" and "time:p"
        }

        public void SetupTimeZoneManager(string locationName, SteadyClockTimePoint timeZoneUpdatedTimePoint, uint totalLocationNameCount, UInt128 timeZoneRuleVersion, Stream timeZoneBinaryStream)
        {
            if (TimeZone.Manager.SetDeviceLocationNameWithTimeZoneRule(locationName, timeZoneBinaryStream) != ResultCode.Success)
            {
                throw new InternalServiceException("Cannot set DeviceLocationName with a given TimeZoneBinary");
            }

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

        public void SetupStandardUserSystemClock(bool isAutomaticCorrectionEnabled, SteadyClockTimePoint steadyClockTimePoint)
        {
            if (StandardUserSystemClock.SetAutomaticCorrectionEnabled(isAutomaticCorrectionEnabled) != ResultCode.Success)
            {
                throw new InternalServiceException("Cannot set automatic user time correction state");
            }

            StandardUserSystemClock.SetAutomaticCorrectionUpdatedTime(steadyClockTimePoint);
            StandardUserSystemClock.MarkInitialized();

            SharedMemory.SetAutomaticCorrectionEnabled(isAutomaticCorrectionEnabled);

            // TODO: propagate IPC late binding of "time:s" and "time:p"
        }

        public void SetStandardSteadyClockRtcOffset(TimeSpanType rtcOffset)
        {
            StandardSteadyClock.SetSetupValue(rtcOffset);

            TimeSpanType currentTimePoint = StandardSteadyClock.GetCurrentRawTimePoint();

            SharedMemory.SetSteadyClockRawTimePoint(currentTimePoint);
        }
    }
}
