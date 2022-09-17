namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Spacemeowx2Ldn.Proxy
{
    internal interface ILdnTcpSocket : ILdnSocket
    {
        bool Connect();

        bool ConnectAsync();

        void DisconnectAndStop();
    }
}
