using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Network.Types;
using System;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.RyuLdn
{
    interface INetworkClient : IDisposable
    {
        event EventHandler<NetworkChangeEventArgs> NetworkChange;

        void DisconnectNetwork();

        void DisconnectAndStop();

        bool Connect(ConnectRequest request);
        void SetStationAcceptPolicy(byte acceptPolicy);
        bool CreateNetwork(CreateAccessPointRequest request, byte[] advertiseData);
        NetworkInfo[] Scan(uint channel, uint bufferCount, ScanFilter scanFilter);
    }
}
