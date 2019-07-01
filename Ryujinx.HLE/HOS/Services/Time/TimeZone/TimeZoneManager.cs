using LibHac.Fs.NcaUtils;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.FileSystem.Content;
using System;

using static Ryujinx.HLE.HOS.Services.Time.TimeZoneRule;
using static Ryujinx.HLE.HOS.ErrorCode;

namespace Ryujinx.HLE.HOS.Services.Time.TimeZone
{
    public sealed class TimeZoneManager
    {
        private const long TimeZoneBinaryTitleId = 0x010000000000080E;

        private static TimeZoneManager instance = null;

        private static object instanceLock = new object();


        private ContentManager _contentManager;
        private TimeZoneRule  _myRules;

        TimeZoneManager()
        {
            _contentManager = null;

            // Empty rules
            _myRules = new TimeZoneRule
            {
                ats   = new long[TZ_MAX_TIMES],
                types = new byte[TZ_MAX_TIMES],
                ttis  = new TimeTypeInfo[TZ_MAX_TYPES],
                chars = new char[TZ_NAME_MAX]
            };
        }

        internal void Initialize(ContentManager contentManager)
        {
            _contentManager = contentManager;
        }

        public string GetTimeZoneBinaryTitleContentPath()
        {
            return _contentManager.GetInstalledContentPath(TimeZoneBinaryTitleId, StorageId.NandSystem, ContentType.Data);
        }

        public bool HasTimeZoneBinaryTitle()
        {
            return !string.IsNullOrEmpty(GetTimeZoneBinaryTitleContentPath());
        }

        public uint LoadTimeZoneRules(out TimeZoneRule outRules, string locationName)
        {
            outRules = new TimeZoneRule
            {
                ats   = new long[TZ_MAX_TIMES],
                types = new byte[TZ_MAX_TIMES],
                ttis  = new TimeTypeInfo[TZ_MAX_TYPES],
                chars = new char[TZ_NAME_MAX]
            };

            if (!HasTimeZoneBinaryTitle())
            {
                Logger.PrintWarning(LogClass.ServiceTime, "TimeZoneBinary system archive not found! Time conversion might not be accurate!");
                try
                {
                    TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById(locationName);

                    // TODO convert TimeZoneInfo to a TimeZoneRule
                    throw new NotImplementedException();
                }
                catch (TimeZoneNotFoundException)
                {
                    Logger.PrintWarning(LogClass.ServiceTime, $"Timezone not found for string: {locationName})");

                    return MakeError(ErrorModule.Time, TimeError.TimeZoneNotFound);
                }
            }
            else
            {
                // TODO: system archive loading
                throw new NotImplementedException();
            }
        }

        public uint ToCalendarTimeWithMyRules(long time, out CalendarInfo calendar)
        {
            return ToCalendarTime(_myRules, time, out calendar);
        }

        public static uint ToCalendarTime(TimeZoneRule rules, long time, out CalendarInfo calendar)
        {
            int error = TimeZone.ToCalendarTime(rules, time, out calendar);

            if (error != 0)
            {
                return MakeError(ErrorModule.Time, error);
            }

            return 0;
        }

        public uint ToPosixTimeWithMyRules(CalendarTime calendarTime, out long posixTime)
        {
            return ToPosixTime(_myRules, calendarTime, out posixTime);
        }

        public static uint ToPosixTime(TimeZoneRule rules, CalendarTime calendarTime, out long posixTime)
        {
            int error = TimeZone.ToPosixTime(rules, calendarTime, out posixTime);

            if (error != 0)
            {
                return MakeError(ErrorModule.Time, error);
            }

            return 0;
        }

        public static TimeZoneManager Instance
        {
            get
            {
                lock (instanceLock)
                {
                    if (instance == null)
                    {
                        instance = new TimeZoneManager();
                    }

                    return instance;
                }
            }
        }
    }
}
