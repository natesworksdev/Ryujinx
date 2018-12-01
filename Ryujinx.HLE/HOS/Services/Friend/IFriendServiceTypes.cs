namespace Ryujinx.HLE.HOS.Services.Friend
{
    internal enum PresenceStatusFilter
    {
        None,
        Online,
        OnlinePlay,
        OnlineOrOnlinePlay
    }

    internal struct FriendFilter
    {
        public PresenceStatusFilter PresenceStatus;
        public bool                 IsFavoriteOnly;
        public bool                 IsSameAppPresenceOnly;
        public bool                 IsSameAppPlayedOnly;
        public bool                 IsArbitraryAppPlayedOnly;
        public long                 PresenceGroupId;
    }
}
