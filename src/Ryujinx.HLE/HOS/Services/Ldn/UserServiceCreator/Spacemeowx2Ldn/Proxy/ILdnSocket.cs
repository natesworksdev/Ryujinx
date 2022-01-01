using System.Net;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Spacemeowx2Ldn.Proxy
{
    internal interface ILdnSocket
    {
        bool SendPacketAsync(EndPoint endpoint, byte[] buffer);

        bool Start();

        bool Stop();

        void Dispose();
    }
}
