using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Network.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.RyuLdn.Types;
using System;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.RyuLdn
{
    interface INetworkClient : IDisposable
    {
        event EventHandler<NetworkChangeEventArgs> NetworkChange;

        void DisconnectNetwork();

        void DisconnectAndStop();

        NetworkError Connect(ConnectRequest request);

        void SetStationAcceptPolicy(AcceptPolicy acceptPolicy);

        void SetAdvertiseData(byte[] data);

        bool CreateNetwork(CreateAccessPointRequest request, byte[] advertiseData);

        NetworkInfo[] Scan(ushort channel, ScanFilter scanFilter);
    }
}