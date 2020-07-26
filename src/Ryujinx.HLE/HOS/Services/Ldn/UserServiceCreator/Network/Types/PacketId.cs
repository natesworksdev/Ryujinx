namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Network.Types
{
    enum PacketId
    {
        CreateAccessPoint,
        SyncNetwork,
        Scan,
        ScanReply,
        ScanReplyEnd,
        Connect,
        Connected,
        Disconnect,
    }
}
