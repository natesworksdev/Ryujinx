using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Network.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.RyuLdn;
using System;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator
{
    class DummyLdnClient : INetworkClient
    {
        public event EventHandler<NetworkChangeEventArgs> NetworkChange;

        public bool Connect(ConnectRequest request)
        {
            NetworkChange?.Invoke(this, new NetworkChangeEventArgs(new NetworkInfo(), false));

            return true;
        }

        public bool CreateNetwork(CreateAccessPointRequest request, byte[] advertiseData)
        {
            NetworkChange?.Invoke(this, new NetworkChangeEventArgs(new NetworkInfo(), false));

            return true;
        }

        public void DisconnectAndStop()
        {

        }

        public void DisconnectNetwork()
        {

        }

        public NetworkInfo[] Scan(uint channel, uint bufferCount, ScanFilter scanFilter)
        {
            return Array.Empty<NetworkInfo>();
        }

        public void SetAdvertiseData(byte[] data)
        {

        }

        public void SetStationAcceptPolicy(byte acceptPolicy)
        {

        }

        public void Dispose()
        {

        }
    }
}
