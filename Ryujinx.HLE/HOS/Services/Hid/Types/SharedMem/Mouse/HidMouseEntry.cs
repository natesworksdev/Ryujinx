
namespace Ryujinx.HLE.HOS.Services.Hid
{
    public struct HidMouseEntry
    {
        public ulong SampleTimestamp;
        public ulong SampleTimestamp2;
        public MousePosition Position;
        public ulong Buttons;
    }
}