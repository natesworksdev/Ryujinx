namespace Ryujinx.HLE.HOS.Services.Ldn.Types
{
    enum DisconnectReason
    {
        None,
        DisconnectedByUser,
        DisconnectedBySystem,
        DestroyedByUser,
        DestroyedBySystem,
        Rejected,
        SignalLost
    }
}