using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Time
{
    [StructLayout(LayoutKind.Sequential, Size = 0x10, Pack = 4)]
    public struct TimeTypeInfo
    {
        public int gmtOffset;

        [MarshalAs(UnmanagedType.I1)]
        public bool isDaySavingTime;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        char[] padding1;

        public int abbreviationListIndex;

        [MarshalAs(UnmanagedType.I1)]
        public bool isStandardTimeDaylight;

        [MarshalAs(UnmanagedType.I1)]
        public bool isGMT;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        char[] padding2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0x4000, CharSet = CharSet.Ansi)]
    public struct TimeZoneRule
    {
        public const int TZ_MAX_TYPES = 128;
        public const int TZ_MAX_CHARS = 50;
        public const int TZ_MAX_LEAPS = 50;
        public const int TZ_MAX_TIMES = 1000;
        public const int TZNAME_MAX   = 255;
        public const int TZ_NAME_MAX  = 2 * (TZNAME_MAX + 1);

        public int timeCount;
        public int typeCount;
        public int charCount;

        [MarshalAs(UnmanagedType.I1)]
        public bool goBack;

        [MarshalAs(UnmanagedType.I1)]
        public bool goAhead;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = TZ_MAX_TIMES)]
        public long[] ats;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = TZ_MAX_TIMES)]
        public byte[] types;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = TZ_MAX_TYPES)]
        public TimeTypeInfo[] ttis;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = TZ_NAME_MAX)]
        public char[] chars;

        public int defaultType;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0x4, Size = 0x2C)]
    public struct TzifHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] magic;

        public char version;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        public byte[] reserved;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] ttisGMTCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] ttisSTDCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] leapCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] timeCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] typeCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] charCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0x4, Size = 0x8)]
    public struct CalendarTime
    {
        public short year;
        public sbyte month;
        public sbyte day;
        public sbyte hour;
        public sbyte minute;
        public sbyte second;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0x4, Size = 0x18, CharSet = CharSet.Ansi)]
    public struct CalendarAdditionalInfo
    {
        public uint dayOfWeek;
        public uint dayOfYear;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public char[] timezoneName;

        [MarshalAs(UnmanagedType.I1)]
        public bool isDaySavingTime;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        char[] padding;

        public int gmtOffset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0x4, Size = 0x20, CharSet = CharSet.Ansi)]
    public struct CalendarInfo
    {
        public CalendarTime           time;
        public CalendarAdditionalInfo additionalInfo;
    }
}
