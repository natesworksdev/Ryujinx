using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Settings.System
{
    [Flags]
    enum NotificationFlag : uint
    {
        RingtoneFlag = 1 << 0,
        DownloadCompletionFlag = 1 << 1,
        EnablesNews = 1 << 8,
        IncomingLampFlag = 1 << 9,
    }

    enum NotificationVolume : uint
    {
        Mute,
        Low,
        High,
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x18, Pack = 0x4)]
    struct NotificationSettings
    {
        public NotificationFlag Flag;
        public NotificationVolume Volume;
        public ulong HeadTime;
        public ulong TailTime;
    }
}
