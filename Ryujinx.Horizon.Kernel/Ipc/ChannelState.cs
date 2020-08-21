namespace Ryujinx.Horizon.Kernel.Ipc
{
    enum ChannelState
    {
        NotInitialized,
        Open,
        ClientDisconnected,
        ServerDisconnected
    }
}