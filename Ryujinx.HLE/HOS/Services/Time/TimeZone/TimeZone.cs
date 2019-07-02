using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Ryujinx.Common;

using static Ryujinx.HLE.HOS.Services.Time.TimeZoneRule;

namespace Ryujinx.HLE.HOS.Services.Time.TimeZone
{
    public class TimeZone
    {
        private const int TIME_SIZE  = 8;
        private const int EPOCH_YEAR     = 1970;
        private const int YEAR_BASE      = 1900;
        private const int EPOCH_WEEK_DAY = 4;
        private const int SECS_PER_MIN   = 60;
        private const int MINS_PER_HOUR  = 60;
        private const int HOURS_PER_DAY  = 24;
        private const int DAYS_PER_WEEK  = 7;
        private const int DAYS_PER_NYEAR = 365;
        private const int DAYS_PER_LYEAR = 366;
        private const int MONS_PER_YEAR  = 12;
        private const int SECS_PER_HOUR  = SECS_PER_MIN * MINS_PER_HOUR;
        private const int SECS_PER_DAY   = SECS_PER_HOUR * HOURS_PER_DAY;

        private const int YEARS_PER_REPEAT       = 400;
        private const long AVERAGE_SECS_PER_YEAR = 31556952;
        private const long SECS_PER_REPEAT       = YEARS_PER_REPEAT * AVERAGE_SECS_PER_YEAR;

        private static readonly int[] YEAR_LENGTHS    = { DAYS_PER_NYEAR, DAYS_PER_LYEAR };
        private static readonly int[][] MONTH_LENGTHS = new int[][]
        {
            new int[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 },
            new int[] { 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 }
        };

        private const string GMT             = "GMT\0";
        private const string TZDEFRULESTRING = ",M4.1.0,M10.5.0";

        [StructLayout(LayoutKind.Sequential, Pack = 0x4, Size = 0x10)]
        private struct CalendarTimeInternal
        {
            // NOTE: On the IPC side this is supposed to be a 16 bits value but internally this need to be a 64 bits value for ToPosixTime.
            public long  year;
            public sbyte month;
            public sbyte day;
            public sbyte hour;
            public sbyte minute;
            public sbyte second;

            public int CompareTo(CalendarTimeInternal other)
            {
                if (year != other.year)
                {
                    if (year < other.year)
                    {
                        return -1;
                    }

                    return 1;
                }

                if (month != other.month)
                {
                    return month - other.month;
                }

                if (day != other.day)
                {
                    return day - other.day;
                }

                if (hour != other.hour)
                {
                    return hour - other.hour;
                }

                if (minute != other.minute)
                {
                    return minute - other.minute;
                }

                if (second != other.second)
                {
                    return second - other.second;
                }

                return 0;
            }
        }

        private enum RuleType
        {
            JulianDay,
            DayOfYear,
            MonthNthDayOfWeek
        }

        private struct Rule
        {
            public RuleType type;
            public int      day;
            public int      week;
            public int      month;
            public int      transitionTime;
        }

        private static int detzcode32(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes, 0, bytes.Length);
            }

            return BitConverter.ToInt32(bytes, 0);
        }

        private static unsafe int detzcode32(int* data)
        {
            int result = *data;
            if (BitConverter.IsLittleEndian)
            {
                byte[] bytes = BitConverter.GetBytes(result);
                Array.Reverse(bytes, 0, bytes.Length);
                result = BitConverter.ToInt32(bytes, 0);
            }

            return result;
        }

        private static unsafe long detzcode64(long* data)
        {
            long result = *data;
            if (BitConverter.IsLittleEndian)
            {
                byte[] bytes = BitConverter.GetBytes(result);
                Array.Reverse(bytes, 0, bytes.Length);
                result = BitConverter.ToInt64(bytes, 0);
            }

            return result;
        }

        private static bool DifferByRepeat(long t1, long t0)
        {
            return (t1 - t0) == SECS_PER_REPEAT;
        }

        private static unsafe bool TimeTypeEquals(TimeZoneRule outRules, byte aIndex, byte bIndex)
        {
            if (aIndex < 0 || aIndex >= outRules.typeCount || bIndex < 0 || bIndex >= outRules.typeCount)
            {
                return false;
            }

            TimeTypeInfo a = outRules.ttis[aIndex];
            TimeTypeInfo b = outRules.ttis[bIndex];

            fixed (char* chars = outRules.chars)
            {
                return a.gmtOffset == b.gmtOffset && a.isDaySavingTime == b.isDaySavingTime && a.isStandardTimeDaylight == b.isStandardTimeDaylight && a.isGMT == b.isGMT && CompareCStr(chars + a.abbreviationListIndex, chars + b.abbreviationListIndex) == 0;
            }
        }

        private static byte[] StreamToBytes(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }

        private static unsafe int CompareCStr(char* s1, char* s2)
        {
            int s1Index = 0;
            int s2Index = 0;

            while (s1[s1Index] != 0 && s2[s2Index] != 0 && s1[s1Index] == s2[s2Index])
            {
                s1Index += 1;
                s2Index += 1;
            }

            return s2[s2Index] - s1[s1Index];
        }

        private static unsafe int LengthCstr(char* s)
        {
            int i = 0;

            while (s[i] != '\0')
            {
                i++;
            }

            return i;
        }

        private static unsafe char* GetQZName(char* name, char delimiter)
        {
            while (*name != '\0' && *name != delimiter)
            {
                name++;
            }

            return name;
        }

        private static unsafe char* GetTZName(char* name)
        {
            while (*name != '\0' && !char.IsDigit(*name) && *name != ',' && *name != '-' && *name != '+')
            {
                name++;
            }
            return name;
        }

        private static unsafe bool GetNum(ref char* name, out int num, int min, int max)
        {
            num = 0;

            char c = *name;

            if (!char.IsDigit(c))
            {
                return false;
            }

            do
            {
                num = num * 10 + (c - '0');
                if (num > max)
                {
                    return false;
                }

                c = *++name;
            } while (char.IsDigit(c));

            if (num < min)
            {
                return false;
            }

            return true;
        }

        private static unsafe bool GetSeconds(ref char* name, out int seconds)
        {
            seconds = 0;

            int num;

            bool isValid = GetNum(ref name, out num, 0, HOURS_PER_DAY * DAYS_PER_WEEK - 1);
            if (!isValid)
            {
                return false;
            }

            seconds = num * SECS_PER_HOUR;
            if (*name == ':')
            {
                name++;
                isValid = GetNum(ref name, out num, 0, MINS_PER_HOUR - 1);
                if (!isValid)
                {
                    return false;
                }

                seconds += num * SECS_PER_MIN;
                if (*name == ':')
                {
                    name++;
                    isValid = GetNum(ref name, out num, 0, SECS_PER_MIN);
                    if (!isValid)
                    {
                        return false;
                    }

                    seconds += num;
                }
            }
            return true;
        }

        private static unsafe bool GetOffset(ref char* name, ref int offset)
        {
            bool isNegative = false;

            if (*name == '-')
            {
                isNegative = true;
                name++;
            }
            else if (*name == '+')
            {
                name++;
            }

            bool isValid = GetSeconds(ref name, out offset);
            if (!isValid)
            {
                return false;
            }

            if (isNegative)
            {
                offset = -offset;
            }

            return true;
        }

        private static unsafe bool GetRule(ref char* name, out Rule rule)
        {
            rule = new Rule();

            bool isValid = false;

            if (*name == 'J')
            {
                name++;

                rule.type = RuleType.JulianDay;
                isValid = GetNum(ref name, out rule.day, 1, DAYS_PER_NYEAR);
            }
            else if (*name == 'M')
            {
                name++;

                rule.type = RuleType.MonthNthDayOfWeek;
                isValid = GetNum(ref name, out rule.month, 1, MONS_PER_YEAR);

                if (!isValid)
                {
                    return false;
                }

                if (*name++ != '.')
                {
                    return false;
                }

                isValid = GetNum(ref name, out rule.week, 1, 5);
                if (!isValid)
                {
                    return false;
                }

                if (*name++ != '.')
                {
                    return false;
                }

                isValid = GetNum(ref name, out rule.day, 0, DAYS_PER_WEEK - 1);
            }
            else if (char.IsDigit(*name))
            {
                rule.type = RuleType.DayOfYear;
                isValid = GetNum(ref name, out rule.day, 0, DAYS_PER_LYEAR - 1);
            }
            else
            {
                return false;
            }

            if (!isValid)
            {
                return false;
            }

            if (*name == '/')
            {
                name++;
                return GetOffset(ref name, ref rule.transitionTime);
            }
            else
            {
                rule.transitionTime = 2 * SECS_PER_HOUR;
            }

            return true;
        }

        private static int IsLeap(int year)
        {
            if (((year) % 4) == 0 && (((year) % 100) != 0 || ((year) % 400) == 0))
            {
                return 1;
            }

            return 0;
        }

        private static unsafe bool ParsePosixName(char* name, out TimeZoneRule outRules, bool lastDitch)
        {
            outRules = new TimeZoneRule
            {
                ats   = new long[TZ_MAX_TIMES],
                types = new byte[TZ_MAX_TIMES],
                ttis  = new TimeTypeInfo[TZ_MAX_TYPES],
                chars = new char[TZ_NAME_MAX]
            };

            int stdLen;

            char* stdName = name;
            int stdOffset = 0;

            if (lastDitch)
            {
                stdLen = 3;
                name += stdLen;
            }
            else
            {
                if (*name == '<')
                {
                    name++;
                    stdName = name;
                    name = GetQZName(name, '>');
                    if (*name != '>')
                    {
                        return false;
                    }
                    stdLen = (int)(name - stdName);
                    name++;
                }
                else
                {
                    name = GetTZName(name);
                    stdLen = (int)(name - stdName);
                }

                if (stdLen == 0)
                {
                    return false;
                }
                bool isValid = GetOffset(ref name, ref stdOffset);
                if (!isValid)
                {
                    return false;
                }
            }

            int charCount = (int)stdLen + 1;
            int destLen   = 0;
            int dstOffset = 0;

            char* destName = name;

            if (TZ_NAME_MAX < charCount)
            {
                return false;
            }

            if (*name != '\0')
            {
                if (*name == '<')
                {
                    destName = ++name;
                    name = GetQZName(name, '>');
                    if (*name != '>')
                    {
                        return false;
                    }
                    destLen = (int)(name - destName);
                    name++;
                }
                else
                {
                    destName = name;
                    name     = GetTZName(name);
                    destLen  = (int)(name - destName);
                }

                if (destLen == 0)
                {
                    return false;
                }

                charCount += (int)destLen + 1;
                if (TZ_NAME_MAX < charCount)
                {
                    return false;
                }

                if (*name != '\0' && *name != ',' && *name != ';')
                {
                    bool isValid = GetOffset(ref name, ref dstOffset);
                    if (!isValid)
                    {
                        return false;
                    }
                }
                else
                {
                    dstOffset = stdOffset - SECS_PER_HOUR;
                }

                if (*name == '\0')
                {
                    fixed (char* defaultTz = TZDEFRULESTRING.ToCharArray())
                    {
                        name = defaultTz;
                    }
                }

                if (*name == ',' || *name == ';')
                {
                    name++;

                    bool IsRuleValid = GetRule(ref name, out Rule start);
                    if (!IsRuleValid)
                    {
                        return false;
                    }

                    if (*name++ != ',')
                    {
                        return false;
                    }

                    IsRuleValid = GetRule(ref name, out Rule end);
                    if (!IsRuleValid)
                    {
                        return false;
                    }

                    if (*name != '\0')
                    {
                        return false;
                    }

                    outRules.typeCount = 2;

                    outRules.ttis[0] = new TimeTypeInfo
                    {
                        gmtOffset             = -dstOffset,
                        isDaySavingTime       = true,
                        abbreviationListIndex = stdLen + 1
                    };

                    outRules.ttis[1] = new TimeTypeInfo
                    {
                        gmtOffset             = -stdOffset,
                        isDaySavingTime       = false,
                        abbreviationListIndex = 0
                    };

                    outRules.defaultType = 0;

                    int timeCount    = 0;
                    long janFirst    = 0;
                    int janOffset    = 0;
                    int yearBegining = EPOCH_YEAR;

                    do
                    {
                        int yearSeconds = YEAR_LENGTHS[IsLeap(yearBegining - 1)] * SECS_PER_DAY;
                        yearBegining--;
                        if (IncrementOverflow64(ref janFirst, -yearSeconds))
                        {
                            janOffset = -yearSeconds;
                            break;
                        }
                    }
                    while (EPOCH_YEAR - YEARS_PER_REPEAT / 2 < yearBegining);

                    int yearLimit = yearBegining + YEARS_PER_REPEAT + 1;
                    int year;
                    for (year = yearBegining; year < yearLimit; year++)
                    {
                        int startTime = TransitionTime(year, start, stdOffset);
                        int endTime   = TransitionTime(year, end, dstOffset);

                        int yearSeconds = YEAR_LENGTHS[IsLeap(year)] * SECS_PER_DAY;

                        bool isReversed = endTime < startTime;
                        if (isReversed)
                        {
                            int swap = startTime;

                            startTime = endTime;
                            endTime   = swap;
                        }

                        if (isReversed || (startTime < endTime && (endTime - startTime < (yearSeconds + (stdOffset - dstOffset)))))
                        {
                            if (TZ_MAX_TIMES - 2 < timeCount)
                            {
                                break;
                            }

                            outRules.ats[timeCount] = janFirst;
                            if (!IncrementOverflow64(ref outRules.ats[timeCount], janOffset + startTime))
                            {
                                outRules.types[timeCount++] = isReversed ? (byte)1 : (byte)0;
                            }
                            else if (janOffset != 0)
                            {
                                outRules.defaultType = isReversed ? 1 : 0;
                            }

                            outRules.ats[timeCount] = janFirst;
                            if (!IncrementOverflow64(ref outRules.ats[timeCount], janOffset + endTime))
                            {
                                outRules.types[timeCount++] = isReversed ? (byte)0 : (byte)1;
                                yearLimit = year + YEARS_PER_REPEAT + 1;
                            }
                            else if (janOffset != 0)
                            {
                                outRules.defaultType = isReversed ? 0 : 1;
                            }
                        }

                        if (IncrementOverflow64(ref janFirst, janOffset + yearSeconds))
                        {
                            break;
                        }

                        janOffset = 0;
                    }

                    outRules.timeCount = timeCount;

                    // There is no time variation, this is then a perpetual DST rule
                    if (timeCount == 0)
                    {
                        outRules.typeCount = 1;
                    }
                    else if (YEARS_PER_REPEAT < year - yearBegining)
                    {
                        outRules.goBack  = true;
                        outRules.goAhead = true;
                    }
                }
                else
                {
                    if (*name == '\0')
                    {
                        return false;
                    }

                    long theirStdOffset = 0;
                    for (int i = 0; i < outRules.timeCount; i++)
                    {
                        int j = outRules.types[i];
                        if (outRules.ttis[j].isStandardTimeDaylight)
                        {
                            theirStdOffset = -outRules.ttis[j].gmtOffset;
                        }
                    }

                    long theirDstOffset = 0;
                    for (int i = 0; i < outRules.timeCount; i++)
                    {
                        int j = outRules.types[i];
                        if (outRules.ttis[j].isDaySavingTime)
                        {
                            theirDstOffset = -outRules.ttis[j].gmtOffset;
                        }
                    }

                    bool isDaySavingTime = false;
                    long theirOffset = theirStdOffset;
                    for (int i = 0; i < outRules.timeCount; i++)
                    {
                        int j = outRules.types[i];
                        outRules.types[i] = outRules.ttis[j].isDaySavingTime ? (byte)1 : (byte)0;
                        if (!outRules.ttis[j].isGMT)
                        {
                            if (isDaySavingTime && !outRules.ttis[j].isStandardTimeDaylight)
                            {
                                outRules.ats[i] += dstOffset - theirStdOffset;
                            }
                            else
                            {
                                outRules.ats[i] += stdOffset - theirStdOffset;
                            }
                        }

                        theirOffset = -outRules.ttis[j].gmtOffset;
                        if (outRules.ttis[j].isDaySavingTime)
                        {
                            theirDstOffset = theirOffset;
                        }
                        else
                        {
                            theirStdOffset = theirOffset;
                        }
                    }

                    outRules.ttis[0] = new TimeTypeInfo
                    {
                        gmtOffset             = -stdOffset,
                        isDaySavingTime       = false,
                        abbreviationListIndex = 0
                    };
                    outRules.ttis[1] = new TimeTypeInfo
                    {
                        gmtOffset             = -dstOffset,
                        isDaySavingTime       = true,
                        abbreviationListIndex = stdLen + 1
                    };

                    outRules.typeCount   = 2;
                    outRules.defaultType = 0;
                }
            }
            else
            {
                // default is perpetual standard time
                outRules.typeCount = 1;
                outRules.timeCount = 0;
                outRules.ttis[0]   = new TimeTypeInfo
                {
                    gmtOffset             = -stdOffset,
                    isDaySavingTime       = false,
                    abbreviationListIndex = 0
                };

                outRules.defaultType = 0;
            }

            outRules.charCount = charCount;

            fixed (char* chars = outRules.chars)
            {
                char* cp = chars;

                for (int i = 0; i < stdLen; i++)
                {
                    cp[i] = stdName[i];
                }
                cp += stdLen;
                *cp++ = '\0';

                if (destLen != 0)
                {
                    for (int i = 0; i < destLen; i++)
                    {
                        cp[i] = destName[i];
                    }
                    *(cp + destLen) = '\0';
                }
            }

            return true;
        }

        private static int TransitionTime(int year, Rule rule, int offset)
        {
            int leapYear = IsLeap(year);

            int value;
            switch (rule.type)
            {
                case RuleType.JulianDay:
                    value = (rule.day - 1) * SECS_PER_DAY;
                    if (leapYear == 1 && rule.day >= 60)
                    {
                        value += SECS_PER_DAY;
                    }
                    break;

                case RuleType.DayOfYear:
                    value = rule.day * SECS_PER_DAY;
                    break;

                case RuleType.MonthNthDayOfWeek:
                    // Here we use Zeller's Congruence to get the day of week of the first month.

                    int m1  = (rule.month + 9) % 12 + 1;
                    int yy0 = (rule.month <= 2) ? (year - 1) : year;
                    int yy1 = yy0 / 100;
                    int yy2 = yy0 % 100;

                    int dayOfWeek = ((26 * m1 - 2) / 10 + 1 + yy2 + yy2 / 4 + yy1 / 4 - 2 * yy1) % 7;

                    if (dayOfWeek < 0)
                    {
                        dayOfWeek += DAYS_PER_WEEK;
                    }

                    // Get the zero origin
                    int d = rule.day - dayOfWeek;

                    if (d < 0)
                    {
                        d += DAYS_PER_WEEK;
                    }

                    for (int i = 1; i < rule.week; i++)
                    {
                        if (d + DAYS_PER_WEEK >= MONTH_LENGTHS[leapYear][rule.month - 1])
                        {
                            break;
                        }

                        d += DAYS_PER_WEEK;
                    }

                    value = d * SECS_PER_DAY;
                    for (int i = 0; i < rule.month - 1; i++)
                    {
                        value += MONTH_LENGTHS[leapYear][i] * SECS_PER_DAY;
                    }

                    break;
                default:
                    throw new NotImplementedException("Unknown time transition!");

            }

            return value + rule.transitionTime + offset;
        }

        private static bool NormalizeOverflow32(ref int ip, ref int unit, int baseValue)
        {
            int delta;

            if (unit >= 0)
            {
                delta = unit / baseValue;
            }
            else
            {
                delta = -1 - (-1 - unit) / baseValue;
            }

            unit -= delta * baseValue;

            return IncrementOverflow32(ref ip, delta);
        }

        private static bool NormalizeOverflow64(ref long ip, ref long unit, long baseValue)
        {
            long delta;

            if (unit >= 0)
            {
                delta = unit / baseValue;
            }
            else
            {
                delta = -1 - (-1 - unit) / baseValue;
            }

            unit -= delta * baseValue;

            return IncrementOverflow64(ref ip, delta);
        }

        private static bool IncrementOverflow32(ref int time, int j)
        {
            try
            {
                time = checked(time + j);
                return false;
            }
            catch (OverflowException)
            {
                return true;
            }
        }

        private static bool IncrementOverflow64(ref long time, long j)
        {
            try
            {
                time = checked(time + j);
                return false;
            }
            catch (OverflowException)
            {
                return true;
            }
        }

        public static bool ParsePosixName(string name, out TimeZoneRule outRules)
        {
            unsafe
            {
                fixed (char *namePtr = name.ToCharArray())
                {
                    return ParsePosixName(namePtr, out outRules, false);
                }
            }
        }

        public static unsafe bool LoadTimeZoneRules(out TimeZoneRule outRules, Stream inputData)
        {
            outRules = new TimeZoneRule
            {
                ats   = new long[TZ_MAX_TIMES],
                types = new byte[TZ_MAX_TIMES],
                ttis  = new TimeTypeInfo[TZ_MAX_TYPES],
                chars = new char[TZ_NAME_MAX]
            };

            BinaryReader reader = new BinaryReader(inputData);
            long streamLength = reader.BaseStream.Length;

            if (streamLength < Marshal.SizeOf<TzifHeader>())
            {
                return false;
            }

            TzifHeader header = reader.ReadStruct<TzifHeader>();
            streamLength -= Marshal.SizeOf<TzifHeader>();

            int ttisGMTCount = detzcode32(header.ttisGMTCount);
            int ttisSTDCount = detzcode32(header.ttisSTDCount);
            int leapCount    = detzcode32(header.leapCount);
            int timeCount    = detzcode32(header.timeCount);
            int typeCount    = detzcode32(header.typeCount);
            int charCount    = detzcode32(header.charCount);

            if (!(0 <= leapCount
                && leapCount < TZ_MAX_LEAPS
                && 0 < typeCount
                && typeCount < TZ_MAX_TYPES
                && 0 <= timeCount
                && timeCount < TZ_MAX_TIMES
                && 0 <= charCount
                && charCount < TZ_MAX_CHARS
                && (ttisSTDCount == typeCount || ttisSTDCount == 0)
                && (ttisGMTCount == typeCount || ttisGMTCount == 0)))
            {
                return false;
            }


            if (streamLength < (timeCount * TIME_SIZE
                                 + timeCount
                                 + typeCount * 6
                                 + charCount
                                 + leapCount * (TIME_SIZE + 4)
                                 + ttisSTDCount
                                 + ttisGMTCount))
            {
                return false;
            }

            outRules.timeCount = timeCount;
            outRules.typeCount = typeCount;
            outRules.charCount = charCount;

            byte[] workBuffer = StreamToBytes(inputData);

            timeCount = 0;

            fixed (byte* workBufferPtrStart = workBuffer)
            {
                byte* p = workBufferPtrStart;
                for (int i = 0; i < outRules.timeCount; i++)
                {
                    long at = detzcode64((long*)p);
                    outRules.types[i] = 1;

                    if (timeCount != 0 && at <= outRules.ats[timeCount - 1])
                    {
                        if (at < outRules.ats[timeCount - 1])
                        {
                            return false;
                        }

                        outRules.types[i - 1] = 0;
                        timeCount--;
                    }

                    outRules.ats[timeCount++] = at;

                    p += TIME_SIZE;
                }

                timeCount = 0;
                for (int i = 0; i < outRules.timeCount; i++)
                {
                    byte type = *p++;
                    if (outRules.typeCount <= type)
                    {
                        return false;
                    }

                    if (outRules.types[i] != 0)
                    {
                        outRules.types[timeCount++] = type;
                    }
                }

                outRules.timeCount = timeCount;

                for (int i = 0; i < outRules.typeCount; i++)
                {
                    TimeTypeInfo ttis = outRules.ttis[i];
                    ttis.gmtOffset = detzcode32((int*)p);
                    p += 4;

                    if (*p >= 2)
                    {
                        return false;
                    }

                    ttis.isDaySavingTime = *p != 0;
                    p++;

                    int abbreviationListIndex = *p++;
                    if (abbreviationListIndex >= outRules.charCount)
                    {
                        return false;
                    }

                    ttis.abbreviationListIndex = abbreviationListIndex;

                    outRules.ttis[i] = ttis;
                }

                fixed (char* chars = outRules.chars)
                {
                    Encoding.ASCII.GetChars(p, outRules.charCount, chars, outRules.charCount);
                }

                p += outRules.charCount;
                outRules.chars[outRules.charCount] = '\0';


                for (int i = 0; i < outRules.typeCount; i++)
                {
                    if (ttisSTDCount == 0)
                    {
                        outRules.ttis[i].isStandardTimeDaylight = false;
                    }
                    else
                    {
                        if (*p >= 2)
                        {
                            return false;
                        }

                        outRules.ttis[i].isStandardTimeDaylight = *p++ != 0;
                    }

                }

                for (int i = 0; i < outRules.typeCount; i++)
                {
                    if (ttisSTDCount == 0)
                    {
                        outRules.ttis[i].isGMT = false;
                    }
                    else
                    {
                        if (*p >= 2)
                        {
                            return false;
                        }

                        outRules.ttis[i].isGMT = *p++ != 0;
                    }

                }

                long position = (p - workBufferPtrStart);
                long nread    = streamLength - position;

                if (nread < 0)
                {
                    return false;
                }

                // Nintendo abort in case of a TzIf file with a POSIX TZ Name too long to fit inside a TimeZoneRule.
                // As it's impossible in normal usage to achive this, we also force a crash.
                if (nread > (TZNAME_MAX + 1))
                {
                    throw new InvalidOperationException();
                }

                char[] name = new char[TZNAME_MAX + 1];
                Array.Copy(workBuffer, position, name, 0, nread);

                if (nread > 2 && name[0] == '\n' && name[nread - 1] == '\n' && outRules.typeCount + 2 <= TZ_MAX_TYPES)
                {
                    name[nread - 1] = '\0';

                    fixed (char* namePtr = &name[1])
                    {
                        if (ParsePosixName(namePtr, out TimeZoneRule tempRules, false))
                        {
                            int abbreviationCount = 0;
                            charCount = outRules.charCount;

                            fixed (char* chars = outRules.chars)
                            {
                                for (int i = 0; i < tempRules.typeCount; i++)
                                {
                                    fixed (char* tempChars = tempRules.chars)
                                    {
                                        char* tempAbbreviation = tempChars + tempRules.ttis[i].abbreviationListIndex;
                                        int j;

                                        for (j = 0; j < charCount; j++)
                                        {

                                            if (CompareCStr(chars + j, tempAbbreviation) == 0)
                                            {
                                                tempRules.ttis[i].abbreviationListIndex = j;
                                                abbreviationCount++;
                                                break;
                                            }
                                        }

                                        if (j >= charCount)
                                        {
                                            int abbreviationLength = LengthCstr(tempAbbreviation);
                                            if (j + abbreviationLength < TZ_MAX_CHARS)
                                            {
                                                for (int x = 0; x < abbreviationLength; x++)
                                                {
                                                    chars[j + x] = tempAbbreviation[x];
                                                }

                                                charCount = j + abbreviationLength + 1;
                                                tempRules.ttis[i].abbreviationListIndex = j;
                                                abbreviationCount++;
                                            }
                                        }
                                    }
                                }

                                if (abbreviationCount == tempRules.typeCount)
                                {
                                    outRules.charCount = charCount;

                                    // Remove trailing
                                    while (1 < outRules.timeCount && (outRules.types[outRules.timeCount - 1] == outRules.types[outRules.timeCount - 2]))
                                    {
                                        outRules.timeCount--;
                                    }

                                    int i;

                                    for (i = 0; i < tempRules.timeCount; i++)
                                    {
                                        if (outRules.timeCount == 0 || outRules.ats[outRules.timeCount - 1] < tempRules.ats[i])
                                        {
                                            break;
                                        }
                                    }

                                    while (i < tempRules.timeCount && outRules.timeCount < TZ_MAX_TIMES)
                                    {
                                        outRules.ats[outRules.timeCount] = tempRules.ats[i];
                                        outRules.types[outRules.timeCount] = (byte)(outRules.typeCount + (byte)tempRules.types[i]);
                                        outRules.timeCount++;
                                        i++;
                                    }

                                    for (i = 0; i < tempRules.typeCount; i++)
                                    {
                                        outRules.ttis[outRules.typeCount++] = tempRules.ttis[i];
                                    }
                                }
                            }
                        }
                    }
                }

                if (outRules.typeCount == 0)
                {
                    return false;
                }

                if (outRules.timeCount > 1)
                {
                    for (int i = 1; i < outRules.timeCount; i++)
                    {
                        if (TimeTypeEquals(outRules, outRules.types[i], outRules.types[0]) && DifferByRepeat(outRules.ats[i], outRules.ats[0]))
                        {
                            outRules.goBack = true;
                            break;
                        }
                    }

                    for (int i = outRules.timeCount - 2; i >= 0; i--)
                    {
                        if (TimeTypeEquals(outRules, outRules.types[outRules.timeCount - 1], outRules.types[i]) && DifferByRepeat(outRules.ats[outRules.timeCount - 1], outRules.ats[i]))
                        {
                            outRules.goAhead = true;
                            break;
                        }
                    }
                }

                int defaultType;

                for (defaultType = 0; defaultType < outRules.timeCount; defaultType++)
                {
                    if (outRules.types[defaultType] == 0)
                    {
                        break;
                    }
                }

                defaultType = defaultType < outRules.timeCount ? -1 : 0;

                if (defaultType < 0 && outRules.timeCount > 0 && outRules.ttis[outRules.types[0]].isDaySavingTime)
                {
                    defaultType = outRules.types[0];
                    while (--defaultType >= 0)
                    {
                        if (!outRules.ttis[defaultType].isDaySavingTime)
                        {
                            break;
                        }
                    }
                }

                if (defaultType < 0)
                {
                    defaultType = 0;
                    while (outRules.ttis[defaultType].isDaySavingTime)
                    {
                        if (++defaultType >= outRules.typeCount)
                        {
                            defaultType = 0;
                            break;
                        }
                    }
                }

                outRules.defaultType = defaultType;
            }

            return true;
        }

        private static long GetLeapDaysNotNeg(long year)
        {
            return year / 4 - year / 100 + year / 400;
        }

        private static long GetLeapDays(long year)
        {
            if (year < 0)
            {
                return -1 - GetLeapDaysNotNeg(-1 - year);
            }
            else
            {
                return GetLeapDaysNotNeg(year);
            }
        }

        private static int CreateCalendarTime(long time, int gmtOffset, out CalendarTimeInternal calendarTime, out CalendarAdditionalInfo calendarAdditionalInfo)
        {
            long year             = EPOCH_YEAR;
            long timeDays         = time / SECS_PER_DAY;
            long remainingSeconds = time % SECS_PER_DAY;

            calendarTime           = new CalendarTimeInternal();
            calendarAdditionalInfo = new CalendarAdditionalInfo()
            {
                timezoneName = new char[8]
            };

            while (timeDays < 0 || timeDays >= YEAR_LENGTHS[IsLeap((int)year)])
            {
                long timeDelta = timeDays / DAYS_PER_LYEAR;
                long delta = timeDelta;

                if (delta == 0)
                {
                    delta = timeDays < 0 ? -1 : 1;
                }

                long newYear = year;

                if (IncrementOverflow64(ref newYear, delta))
                {
                    return TimeError.OutOfRange;
                }

                long leapDays = GetLeapDays(newYear - 1) - GetLeapDays(year - 1);
                timeDays -= (newYear - year) * DAYS_PER_NYEAR;
                timeDays -= leapDays;
                year = newYear;
            }

            long dayOfYear = timeDays;
            remainingSeconds += gmtOffset;
            while (remainingSeconds < 0)
            {
                remainingSeconds += SECS_PER_DAY;
                dayOfYear -= 1;
            }

            while (remainingSeconds >= SECS_PER_DAY)
            {
                remainingSeconds -= SECS_PER_DAY;
                dayOfYear += 1;
            }

            while (dayOfYear < 0)
            {
                if (IncrementOverflow64(ref year, -1))
                {
                    return TimeError.OutOfRange;
                }

                dayOfYear += YEAR_LENGTHS[IsLeap((int)year)];
            }

            while (dayOfYear >= YEAR_LENGTHS[IsLeap((int)year)])
            {
                dayOfYear -= YEAR_LENGTHS[IsLeap((int)year)];

                if (IncrementOverflow64(ref year, 1))
                {
                    return TimeError.OutOfRange;
                }
            }

            calendarTime.year                = year;
            calendarAdditionalInfo.dayOfYear = (uint)dayOfYear;

            long dayOfWeek = (EPOCH_WEEK_DAY + ((year - EPOCH_YEAR) % DAYS_PER_WEEK) * (DAYS_PER_NYEAR % DAYS_PER_WEEK) + GetLeapDays(year - 1) - GetLeapDays(EPOCH_YEAR - 1) + dayOfYear) % DAYS_PER_WEEK;
            if (dayOfWeek < 0)
            {
                dayOfWeek += DAYS_PER_WEEK;
            }

            calendarAdditionalInfo.dayOfWeek = (uint)dayOfWeek;

            calendarTime.hour = (sbyte)((remainingSeconds / SECS_PER_HOUR) % SECS_PER_HOUR);
            remainingSeconds %= SECS_PER_HOUR;

            calendarTime.minute = (sbyte)(remainingSeconds / SECS_PER_MIN);
            calendarTime.second = (sbyte)(remainingSeconds % SECS_PER_MIN);

            int[] ip = MONTH_LENGTHS[IsLeap((int)year)];

            while (dayOfYear >= ip[calendarTime.month])
            {
                calendarTime.month += 1;

                dayOfYear -= ip[calendarTime.month];
            }

            calendarTime.day = (sbyte)(dayOfYear + 1);

            calendarAdditionalInfo.isDaySavingTime = false;
            calendarAdditionalInfo.gmtOffset       = gmtOffset;

            return 0;
        }

        private static int ToCalendarTimeInternal(TimeZoneRule rules, long time, out CalendarTimeInternal calendarTime, out CalendarAdditionalInfo calendarAdditionalInfo)
        {
            calendarTime           = new CalendarTimeInternal();
            calendarAdditionalInfo = new CalendarAdditionalInfo()
            {
                timezoneName = new char[8]
            };

            int result = 0;

            if ((rules.goAhead && time < rules.ats[0]) || (rules.goBack && time > rules.ats[rules.timeCount - 1]))
            {
                long newTime = time;

                long seconds;
                long years;

                if (time < rules.ats[0])
                {
                    seconds = rules.ats[0] - time;
                }
                else
                {
                    seconds = time - rules.ats[rules.timeCount - 1];
                }

                seconds -= 1;

                years   = (seconds / SECS_PER_REPEAT + 1) * YEARS_PER_REPEAT;
                seconds = years * AVERAGE_SECS_PER_YEAR;

                if (time < rules.ats[0])
                {
                    newTime += seconds;
                }
                else
                {
                    newTime -= seconds;
                }

                if (newTime < rules.ats[0] && newTime > rules.ats[rules.timeCount - 1])
                {
                    return TimeError.TimeNotFound;
                }

                result = ToCalendarTimeInternal(rules, newTime, out calendarTime, out calendarAdditionalInfo);
                if (result != 0)
                {
                    return result;
                }

                if (time < rules.ats[0])
                {
                    calendarTime.year -= years;
                }
                else
                {
                    calendarTime.year += years;
                }

                return 0;
            }

            int ttiIndex;

            if (rules.timeCount == 0 || time < rules.ats[0])
            {
                ttiIndex = rules.defaultType;
            }
            else
            {
                int low = 1;
                int high = rules.timeCount;

                while (low < high)
                {
                    int mid = (low + high) >> 1;

                    if (time < rules.ats[mid])
                    {
                        high = mid;
                    }
                    else
                    {
                        low = mid + 1;
                    }
                }

                ttiIndex = rules.types[low - 1];
            }

            result = CreateCalendarTime(time, rules.ttis[ttiIndex].gmtOffset, out calendarTime, out calendarAdditionalInfo);

            if (result == 0)
            {
                calendarAdditionalInfo.isDaySavingTime = rules.ttis[ttiIndex].isDaySavingTime;

                unsafe
                {
                    fixed (char* timeZoneAbbreviation = &rules.chars[rules.ttis[ttiIndex].abbreviationListIndex])
                    {
                        int timeZoneSize = Math.Min(LengthCstr(timeZoneAbbreviation), 8);
                        for (int i = 0; i < timeZoneSize; i++)
                        {
                            calendarAdditionalInfo.timezoneName[i] = timeZoneAbbreviation[i];
                        }
                    }
                }
            }

            return result;
        }



        private static int ToPosixTimeInternal(TimeZoneRule rules, CalendarTimeInternal calendarTime, out long posixTime)
        {
            posixTime = 0;

            int hour   = calendarTime.hour;
            int minute = calendarTime.minute;

            if (NormalizeOverflow32(ref hour, ref minute, MINS_PER_HOUR))
            {
                return TimeError.Overflow;
            }

            calendarTime.minute = (sbyte)minute;

            int day = calendarTime.day;
            if (NormalizeOverflow32(ref day, ref hour, HOURS_PER_DAY))
            {
                return TimeError.Overflow;
            }

            calendarTime.day  = (sbyte)day;
            calendarTime.hour = (sbyte)hour;

            long year  = calendarTime.year;
            long month = calendarTime.month;

            if (NormalizeOverflow64(ref year, ref month, MONS_PER_YEAR))
            {
                return TimeError.Overflow;
            }

            calendarTime.month = (sbyte)month;

            if (IncrementOverflow64(ref year, YEAR_BASE))
            {
                return TimeError.Overflow;
            }

            while (day <= 0)
            {
                if (IncrementOverflow64(ref year, -1))
                {
                    return TimeError.Overflow;
                }

                long li = year;

                if (1 < calendarTime.month)
                {
                    li++;
                }

                day += YEAR_LENGTHS[IsLeap((int)li)];
            }

            while (day > DAYS_PER_LYEAR)
            {
                long li = year;

                if (1 < calendarTime.month)
                {
                    li++;
                }

                day -= YEAR_LENGTHS[IsLeap((int)li)];

                if (IncrementOverflow64(ref year, 1))
                {
                    return TimeError.Overflow;
                }
            }

            while (true)
            {
                int i = MONTH_LENGTHS[IsLeap((int)year)][calendarTime.month];

                if (day <= i)
                {
                    break;
                }

                day -= i;
                calendarTime.month += 1;

                if (calendarTime.month >= MONS_PER_YEAR)
                {
                    calendarTime.month = 0;
                    if (IncrementOverflow64(ref year, 1))
                    {
                        return TimeError.Overflow;
                    }
                }
            }

            calendarTime.day = (sbyte)day;

            if (IncrementOverflow64(ref year, -YEAR_BASE))
            {
                return TimeError.Overflow;
            }

            calendarTime.year = year;

            int savedSeconds;

            if (calendarTime.second >= 0 && calendarTime.second < SECS_PER_MIN)
            {
                savedSeconds = 0;
            }
            else if (year + YEAR_BASE < EPOCH_YEAR)
            {
                int second = calendarTime.second;
                if (IncrementOverflow32(ref second, 1 - SECS_PER_MIN))
                {
                    return TimeError.Overflow;
                }

                savedSeconds = second;
                calendarTime.second = 1 - SECS_PER_MIN;
            }
            else
            {
                savedSeconds = calendarTime.second;
                calendarTime.second = 0;
            }

            long low = long.MinValue;
            long high = long.MaxValue;

            while (true)
            {
                long pivot = low / 2 + high / 2;

                if (pivot < low)
                {
                    pivot = low;
                }
                else if (pivot > high)
                {
                    pivot = high;
                }

                int direction;

                int result = ToCalendarTimeInternal(rules, pivot, out CalendarTimeInternal candidateCalendarTime, out _);
                if (result != 0)
                {
                    if (pivot > 0)
                    {
                        direction = 1;
                    }
                    else
                    {
                        direction = -1;
                    }
                }
                else
                {
                    direction = candidateCalendarTime.CompareTo(calendarTime);
                }

                if (direction == 0)
                {
                    long timeResult = pivot + savedSeconds;

                    if ((timeResult < pivot) != (savedSeconds < 0))
                    {
                        return TimeError.Overflow;
                    }

                    posixTime = timeResult;
                    break;
                }
                else
                {
                    if (pivot == low)
                    {
                        if (pivot == long.MaxValue)
                        {
                            return TimeError.TimeNotFound;
                        }

                        pivot += 1;
                        low += 1;
                    }
                    else if (pivot == high)
                    {
                        if (pivot == long.MinValue)
                        {
                            return TimeError.TimeNotFound;
                        }

                        pivot -= 1;
                        high -= 1;
                    }

                    if (low > high)
                    {
                        return TimeError.TimeNotFound;
                    }

                    if (direction > 0)
                    {
                        high = pivot;
                    }
                    else
                    {
                        low = pivot;
                    }
                }
            }

            return 0;
        }

        public static int ToCalendarTime(TimeZoneRule rules, long time, out CalendarInfo calendar)
        {
            int result = ToCalendarTimeInternal(rules, time, out CalendarTimeInternal calendarTime, out CalendarAdditionalInfo calendarAdditionalInfo);

            calendar = new CalendarInfo()
            {
                time         = new CalendarTime()
                {
                    year   = (short)calendarTime.year,
                    month  = calendarTime.month,
                    day    = calendarTime.day,
                    hour   = calendarTime.hour,
                    minute = calendarTime.minute,
                    second = calendarTime.second
                },
                additionalInfo = calendarAdditionalInfo
            };

            return result;
        }

        public static int ToPosixTime(TimeZoneRule rules, CalendarTime calendarTime, out long posixTime)
        {
            CalendarTimeInternal calendarTimeInternal = new CalendarTimeInternal()
            {
                year   = calendarTime.year,
                month  = calendarTime.month,
                day    = calendarTime.day,
                hour   = calendarTime.hour,
                minute = calendarTime.minute,
                second = calendarTime.second
            };

            return ToPosixTimeInternal(rules, calendarTimeInternal, out posixTime);
        }
    }
}
