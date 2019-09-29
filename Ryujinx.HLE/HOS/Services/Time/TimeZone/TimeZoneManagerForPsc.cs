using Ryujinx.HLE.HOS.Services.Time.Clock;
using Ryujinx.HLE.Utilities;
using System.IO;
using System.Threading;
using static Ryujinx.HLE.HOS.Services.Time.TimeZone.TimeZoneRule;

namespace Ryujinx.HLE.HOS.Services.Time.TimeZone
{
    class TimeZoneManagerForPsc
    {
        private bool                 _isInitialized;
        private TimeZoneRule         _myRules;
        private string               _deviceLocationName;
        private UInt128              _timeZoneRuleVersion;
        private uint                 _totalLocationNameCount;
        private SteadyClockTimePoint _timeZoneUpdateTimePoint;
        private object               _lock;

        public TimeZoneManagerForPsc()
        {
            _isInitialized       = false;
            _deviceLocationName  = null;
            _timeZoneRuleVersion = new UInt128();
            _lock                = new object();

            // Empty rules
            _myRules = new TimeZoneRule
            {
                Ats   = new long[TzMaxTimes],
                Types = new byte[TzMaxTimes],
                Ttis  = new TimeTypeInfo[TzMaxTypes],
                Chars = new char[TzCharsArraySize]
            };

            _timeZoneUpdateTimePoint = SteadyClockTimePoint.GetRandom();
        }

        public bool IsInitialized()
        {
            Monitor.Enter(_lock);

            bool res = _isInitialized;

            Monitor.Exit(_lock);

            return res;
        }

        public void MarkInitialized()
        {
            Monitor.Enter(_lock);

            _isInitialized = true;

            Monitor.Exit(_lock);
        }

        public ResultCode GetDeviceLocationName(out string deviceLocationName)
        {
            ResultCode result = ResultCode.UninitializedClock;

            deviceLocationName = null;

            Monitor.Enter(_lock);

            if (_isInitialized)
            {
                deviceLocationName = _deviceLocationName;
                result             = ResultCode.Success;
            }

            Monitor.Exit(_lock);

            return result;
        }

        public ResultCode SetDeviceLocationNameWithTimeZoneRule(string locationName, Stream timeZoneBinaryStream)
        {
            ResultCode result = ResultCode.TimeZoneConversionFailed;

            Monitor.Enter(_lock);

            bool timeZoneConversionSuccess = TimeZone.ParseTimeZoneBinary(out TimeZoneRule rules, timeZoneBinaryStream);

            if (timeZoneConversionSuccess)
            {
                _deviceLocationName = locationName;
                _myRules            = rules;
                result              = ResultCode.Success;
            }

            Monitor.Exit(_lock);

            return result;
        }

        public void SetTotalLocationNameCount(uint totalLocationNameCount)
        {
            Monitor.Enter(_lock);

            _totalLocationNameCount = totalLocationNameCount;

            Monitor.Exit(_lock);
        }

        public ResultCode GetTotalLocationNameCount(out uint totalLocationNameCount)
        {
            ResultCode result = ResultCode.UninitializedClock;

            totalLocationNameCount = 0;

            Monitor.Enter(_lock);

            if (_isInitialized)
            {
                totalLocationNameCount = _totalLocationNameCount;
                result                 = ResultCode.Success;
            }

            Monitor.Exit(_lock);

            return result;
        }

        public ResultCode SetUpdatedTime(SteadyClockTimePoint timeZoneUpdatedTimePoint, bool bypassUninitialized = false)
        {
            ResultCode result = ResultCode.UninitializedClock;

            Monitor.Enter(_lock);

            if (_isInitialized || bypassUninitialized)
            {
                _timeZoneUpdateTimePoint = timeZoneUpdatedTimePoint;
                result                   = ResultCode.Success;
            }

            Monitor.Exit(_lock);

            return result;
        }

        public ResultCode GetUpdatedTime(out SteadyClockTimePoint timeZoneUpdatedTimePoint)
        {
            ResultCode result;

            Monitor.Enter(_lock);

            if (_isInitialized)
            {
                timeZoneUpdatedTimePoint = _timeZoneUpdateTimePoint;
                result                   = ResultCode.Success;
            }
            else
            {
                timeZoneUpdatedTimePoint = SteadyClockTimePoint.GetRandom();
                result                   = ResultCode.UninitializedClock;
            }

            Monitor.Exit(_lock);

            return result;
        }

        public ResultCode ParseTimeZoneRuleBinary(out TimeZoneRule outRules, Stream timeZoneBinaryStream)
        {
            ResultCode result = ResultCode.Success;

            Monitor.Enter(_lock);

            bool timeZoneConversionSuccess = TimeZone.ParseTimeZoneBinary(out outRules, timeZoneBinaryStream);

            if (!timeZoneConversionSuccess)
            {
                result = ResultCode.TimeZoneConversionFailed;
            }

            Monitor.Exit(_lock);

            return result;
        }

        public void SetTimeZoneRuleVersion(UInt128 timeZoneRuleVersion)
        {
            Monitor.Enter(_lock);
            _timeZoneRuleVersion = timeZoneRuleVersion;
            Monitor.Exit(_lock);
        }

        public ResultCode GetTimeZoneRuleVersion(out UInt128 timeZoneRuleVersion)
        {
            ResultCode result;

            Monitor.Enter(_lock);

            if (_isInitialized)
            {
                timeZoneRuleVersion = _timeZoneRuleVersion;
                result              = ResultCode.Success;
            }
            else
            {
                timeZoneRuleVersion = new UInt128();
                result              = ResultCode.UninitializedClock;
            }

            Monitor.Exit(_lock);

            return result;
        }

        public ResultCode ToCalendarTimeWithMyRules(long time, out CalendarInfo calendar)
        {
            ResultCode result;

            Monitor.Enter(_lock);

            if (_isInitialized)
            {
                result = ToCalendarTime(_myRules, time, out calendar);
            }
            else
            {
                calendar = new CalendarInfo();
                result   = ResultCode.UninitializedClock;
            }

            Monitor.Exit(_lock);

            return result;
        }

        public ResultCode ToCalendarTime(TimeZoneRule rules, long time, out CalendarInfo calendar)
        {
            Monitor.Enter(_lock);

            ResultCode result = TimeZone.ToCalendarTime(rules, time, out calendar);

            Monitor.Exit(_lock);

            return result;
        }

        public ResultCode ToPosixTimeWithMyRules(CalendarTime calendarTime, out long posixTime)
        {
            ResultCode result;

            Monitor.Enter(_lock);

            if (_isInitialized)
            {
                result = ToPosixTime(_myRules, calendarTime, out posixTime);
            }
            else
            {
                posixTime = 0;
                result    = ResultCode.UninitializedClock;
            }

            Monitor.Exit(_lock);

            return result;
        }

        public ResultCode ToPosixTime(TimeZoneRule rules, CalendarTime calendarTime, out long posixTime)
        {
            Monitor.Enter(_lock);

            ResultCode result = TimeZone.ToPosixTime(rules, calendarTime, out posixTime);

            Monitor.Exit(_lock);

            return result;
        }
    }
}
