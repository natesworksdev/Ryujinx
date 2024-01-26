using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Settings.System
{
    [StructLayout(LayoutKind.Sequential, Size = 0xC, Pack = 0x4)]
    struct SleepSettings
    {
        public uint Flags;
        public uint HandheldSleepPlan;
        public uint ConsoleSleepPlan;
    }
}
