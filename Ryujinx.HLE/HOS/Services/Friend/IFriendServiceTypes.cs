using Ryujinx.HLE.Utilities;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Friend
{
    enum PresenceStatusFilter : uint
    {
        None,
        Online,
        OnlinePlay,
        OnlineOrOnlinePlay
    }

    enum PresenceStatus : uint
    {
        Offline,
        Online,
        OnlinePlay,
    }

    [StructLayout(LayoutKind.Sequential)]
    struct FriendFilter
    {
        public PresenceStatusFilter PresenceStatus;

        [MarshalAs(UnmanagedType.I1)]
        public bool IsFavoriteOnly;

        [MarshalAs(UnmanagedType.I1)]
        public bool IsSameAppPresenceOnly;

        [MarshalAs(UnmanagedType.I1)]
        public bool IsSameAppPlayedOnly;

        [MarshalAs(UnmanagedType.I1)]
        public bool IsArbitraryAppPlayedOnly;

        public long PresenceGroupId;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct UserPresence
    {
        public UInt128        UserId;
        public long           LastTimeOnlineTimestamp;
        public PresenceStatus Status;

        uint padding;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xC0)]
        public char[] AppKeyValueStorage;


        public override string ToString()
        {
            return $"UserPresence {{ UserId: {UserId}, LastTimeOnlineTimestamp: {LastTimeOnlineTimestamp}, Status: {Status}, AppKeyValueStorage: {AppKeyValueStorage} }}";
        }
    }
}
