using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Settings.System
{
    [StructLayout(LayoutKind.Sequential, Size = 0x18, Pack = 0x4)]
    struct NotificationSettings
    {
        public uint Flag;
        public float Volume;
        public ulong HeadTime;
        public ulong TailTime;
    }
}
