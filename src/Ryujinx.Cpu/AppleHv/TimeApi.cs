using System.Runtime.InteropServices;

namespace Ryujinx.Cpu.AppleHv
{
    struct mach_timebase_info_t
    {
        public uint numer;
        public uint denom;
    }

    static partial class TimeApi
    {
        [LibraryImport("libc", SetLastError = true)]
        public static partial ulong mach_absolute_time();

        [LibraryImport("libc", SetLastError = true)]
        public static partial int mach_timebase_info(out mach_timebase_info_t info);
    }
}
