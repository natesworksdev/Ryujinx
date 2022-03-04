using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Network.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.RyuLdn;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.RyuLdn.Types;
using System;
using System.Net;
using System.Net.NetworkInformation;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Spacemeowx2Ldn
{
    internal class Spacemeowx2LdnClient : INetworkClient, IDisposable
    {
        public IUserLocalCommunicationService commService;
        private HLEConfiguration _config;

        public event EventHandler<NetworkChangeEventArgs> NetworkChange;

        protected LanDiscovery lanDiscovery;

        // TODO: Remove debug stuff
        private void LogMsg(string msg)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, msg);
        }

        public Spacemeowx2LdnClient(IUserLocalCommunicationService parent, HLEConfiguration config)
        {
            LogMsg("Init Spacemeowx2LdnClient...");

            commService = parent;
            _config = config;

            UnicastIPAddressInformation localIpInterface = NetworkHelpers.GetLocalInterface(_config.MultiplayerLanInterfaceId).Item2;

            lanDiscovery = new LanDiscovery(this, localIpInterface.Address, localIpInterface.IPv4Mask);


            //Config = new ProxyConfig()
            //{
            //    ProxyIp = NetworkHelpers.ConvertIpv4Address(localAddr),
            //    ProxySubnetMask = NetworkHelpers.ConvertIpv4Address("255.255.255.0")
            //};

            LogMsg("Spacemeowx2LdnClient init done.");
        }

        public NetworkError Connect(ConnectRequest request)
        {
            LogMsg("Spacemeowx2LdnClient Connect");
            return lanDiscovery.Connect(request.NetworkInfo, request.UserConfig, request.LocalCommunicationVersion);
        }

        public NetworkError ConnectPrivate(ConnectPrivateRequest request)
        {
            LogMsg("Spacemeowx2LdnClient ConnectPrivate");
            return NetworkError.None;
        }

        public bool CreateNetwork(CreateAccessPointRequest request, byte[] advertiseData)
        {
            LogMsg("Spacemeowx2LdnClient CreateNetwork");
            return lanDiscovery.CreateNetwork(request.SecurityConfig, request.UserConfig, request.NetworkConfig);
        }

        public bool CreateNetworkPrivate(CreateAccessPointPrivateRequest request, byte[] advertiseData)
        {
            LogMsg("Spacemeowx2LdnClient CreateNetworkPrivate");
            return true;
        }

        public void DisconnectAndStop()
        {
            LogMsg("Spacemeowx2LdnClient DisconnectAndStop");
            lanDiscovery.DisconnectAndStop();
        }

        public void DisconnectNetwork()
        {
            LogMsg("Spacemeowx2LdnClient DisconnectNetwork");
            lanDiscovery.DestroyNetwork();
        }

        public ResultCode Reject(DisconnectReason disconnectReason, uint nodeId)
        {
            LogMsg("Spacemeowx2LdnClient Reject");
            return ResultCode.Success;
        }

        public NetworkInfo[] Scan(ushort channel, ScanFilter scanFilter)
        {
            LogMsg("Spacemeowx2LdnClient Scan");
            return lanDiscovery.Scan(channel, scanFilter);
        }

        public void SetAdvertiseData(byte[] data)
        {
            LogMsg("Spacemeowx2LdnClient SetAdvertiseData");
            lanDiscovery.SetAdvertiseData(data);
        }

        public void SetGameVersion(byte[] versionString)
        {
            LogMsg("Spacemeowx2LdnClient SetGameVersion");
            // Not needed or not implemented?
        }

        public void SetStationAcceptPolicy(AcceptPolicy acceptPolicy)
        {
            LogMsg("Spacemeowx2LdnClient SetStationAcceptPolicy");
            // not implemented
        }

        public void Dispose()
        {
            LogMsg("Spacemeowx2LdnClient Dispose");
            lanDiscovery.Dispose();
        }

        public void HandleCreateNetwork(NetworkInfo info)
        {
            LogMsg("Spacemeowx2LdnClient HandleCreateNetwork");
            NetworkChange?.Invoke(this, new NetworkChangeEventArgs(info, connected: true));
        }

        public void HandleUpdateNodes(NetworkInfo info)
        {
            LogMsg("Spacemeowx2LdnClient HandleUpdateNodes");
            NetworkChange?.Invoke(this, new NetworkChangeEventArgs(info, connected: true));
        }

        public void HandleSyncNetwork(NetworkInfo info)
        {
            LogMsg("Spacemeowx2LdnClient HandleSyncNetwork");
            NetworkChange?.Invoke(this, new NetworkChangeEventArgs(info, connected: true));
        }

        public void HandleConnected(NetworkInfo info)
        {
            LogMsg("Spacemeowx2LdnClient HandleConnected");
            NetworkChange?.Invoke(this, new NetworkChangeEventArgs(info, connected: true));
        }

        public void HandleDisconnected(NetworkInfo info, DisconnectReason reason)
        {
            LogMsg("Spacemeowx2LdnClient HandleDisconnected");
            NetworkChange?.Invoke(this, new NetworkChangeEventArgs(info, connected: false, reason));
        }

        public void HandleDisconnectNetwork(NetworkInfo info, DisconnectReason reason)
        {
            LogMsg("Spacemeowx2LdnClient HandleDisconnectNetwork");
            NetworkChange?.Invoke(this, new NetworkChangeEventArgs(info, connected: false, reason));
        }
    }
}
