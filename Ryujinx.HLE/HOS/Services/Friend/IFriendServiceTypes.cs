namespace Ryujinx.HLE.HOS.Services.Friend
{
    public enum PresenceStatusFilter
    {
        None,
        Online,
        OnlinePlay,
        OnlineOrOnlinePlay
    }

    public struct FriendFilter
    {
        public PresenceStatusFilter PresenceStatus;
        public bool                 IsFavoriteOnly;
        public bool                 IsSameAppPresenceOnly;
        public bool                 IsSameAppPlayedOnly;
        public bool                 IsArbitraryAppPlayedOnly;
        public long                 PresenceGroupId;
    };
}
