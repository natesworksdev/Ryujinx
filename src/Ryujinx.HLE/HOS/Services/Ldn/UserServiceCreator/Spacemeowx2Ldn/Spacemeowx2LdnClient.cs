using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Network.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.RyuLdn;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.RyuLdn.Types;
using System;
using System.Net.NetworkInformation;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Spacemeowx2Ldn
{
    internal class Spacemeowx2LdnClient : INetworkClient, IDisposable
    {
        public event EventHandler<NetworkChangeEventArgs> NetworkChange;

        private LanDiscovery _lanDiscovery;

        public Spacemeowx2LdnClient(HLEConfiguration config)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "Init Spacemeowx2LdnClient...");

            UnicastIPAddressInformation localIpInterface = NetworkHelpers.GetLocalInterface(config.MultiplayerLanInterfaceId).Item2;

            _lanDiscovery = new LanDiscovery(this, localIpInterface.Address, localIpInterface.IPv4Mask);

            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "Spacemeowx2LdnClient init done.");
        }

        internal void InvokeNetworkChange(NetworkInfo info, bool connected, DisconnectReason reason = DisconnectReason.None)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "Spacemeowx2LdnClient NetworkChangeEvent");
            NetworkChange?.Invoke(this, new NetworkChangeEventArgs(info, connected: connected, disconnectReason: reason));
        }

        public NetworkError Connect(ConnectRequest request)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "Spacemeowx2LdnClient Connect");
            return _lanDiscovery.Connect(request.NetworkInfo, request.UserConfig, request.LocalCommunicationVersion);
        }

        public NetworkError ConnectPrivate(ConnectPrivateRequest request)
        {
            Logger.Stub?.PrintMsg(LogClass.ServiceLdn, "Spacemeowx2LdnClient ConnectPrivate");
            return NetworkError.None;
        }

        public bool CreateNetwork(CreateAccessPointRequest request, byte[] advertiseData)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "Spacemeowx2LdnClient CreateNetwork");
            return _lanDiscovery.CreateNetwork(request.SecurityConfig, request.UserConfig, request.NetworkConfig);
        }

        public bool CreateNetworkPrivate(CreateAccessPointPrivateRequest request, byte[] advertiseData)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "Spacemeowx2LdnClient CreateNetworkPrivate");
            return true;
        }

        public void DisconnectAndStop()
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "Spacemeowx2LdnClient DisconnectAndStop");
            _lanDiscovery.DisconnectAndStop();
        }

        public void DisconnectNetwork()
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "Spacemeowx2LdnClient DisconnectNetwork");
            _lanDiscovery.DestroyNetwork();
        }

        public ResultCode Reject(DisconnectReason disconnectReason, uint nodeId)
        {
            Logger.Stub?.PrintMsg(LogClass.ServiceLdn, "Spacemeowx2LdnClient Reject");
            return ResultCode.Success;
        }

        public NetworkInfo[] Scan(ushort channel, ScanFilter scanFilter)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "Spacemeowx2LdnClient Scan");
            return _lanDiscovery.Scan(channel, scanFilter);
        }

        public void SetAdvertiseData(byte[] data)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "Spacemeowx2LdnClient SetAdvertiseData");
            _lanDiscovery.SetAdvertiseData(data);
        }

        public void SetGameVersion(byte[] versionString)
        {
            Logger.Stub?.PrintMsg(LogClass.ServiceLdn, "Spacemeowx2LdnClient SetGameVersion");
        }

        public void SetStationAcceptPolicy(AcceptPolicy acceptPolicy)
        {
            Logger.Stub?.PrintMsg(LogClass.ServiceLdn, "Spacemeowx2LdnClient SetStationAcceptPolicy");
        }

        public void Dispose()
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "Spacemeowx2LdnClient Dispose");
            _lanDiscovery.Dispose();
        }
    }
}
