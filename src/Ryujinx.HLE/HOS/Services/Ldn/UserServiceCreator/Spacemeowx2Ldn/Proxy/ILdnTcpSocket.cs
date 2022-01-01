namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Spacemeowx2Ldn.Proxy
{
    internal interface ILdnTcpSocket : ILdnSocket
    {
        bool ConnectAsync();

        void DisconnectAndStop();
    }
}
