namespace Ryujinx.HLE.HOS.Services.Friend
{
    private enum PresenceStatusFilter
    {
        None,
        Online,
        OnlinePlay,
        OnlineOrOnlinePlay
    }

    private struct FriendFilter
    {
        public PresenceStatusFilter PresenceStatus;
        public bool                 IsFavoriteOnly;
        public bool                 IsSameAppPresenceOnly;
        public bool                 IsSameAppPlayedOnly;
        public bool                 IsArbitraryAppPlayedOnly;
        public long                 PresenceGroupId;
    }
}
