using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Settings.System
{
    [StructLayout(LayoutKind.Sequential, Size = 0x20, Pack = 0x4)]
    struct TvSettings
    {
        public uint Flags;
        public uint TvResolution;
        public uint HdmiContentType;
        public uint RgbRange;
        public uint CmuMode;
        public uint TvUnderscan;
        public uint TvGamma;
        public uint ContrastRatio;
    }
}
