using Ryujinx.Common.Logging;
using Ryujinx.Common.Memory;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.HOS.Services.Ldn.Spacemeowx2Ldn;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.RyuLdn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Spacemeowx2Ldn.Proxy;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Spacemeowx2Ldn
{
    class LanDiscovery : IDisposable
    {
        protected Spacemeowx2LdnClient parent;
        internal LanProtocol protocol;
        internal IPAddress localAddr;
        internal IPAddress localAddrMask;

        private bool inited;
        public readonly Ssid FakeSsid;
        public const int DefaultPort = 11452;
        // Type may need to be changed to ILdnUdpSocket in the future
        protected LdnProxyUdpServer udp;
        protected ILdnTcpSocket tcp;
        protected List<LdnProxyTcpSession> stations = new List<LdnProxyTcpSession>();
        internal NetworkInfo networkInfo;

        private void LogMsg(string msg)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, msg);
        }

        public LanDiscovery(Spacemeowx2LdnClient _parent, IPAddress ipAddr, IPAddress ipv4mask, bool listening = true)
        {
            LogMsg($"Init LanDiscovery using IP: {ipAddr}");

            parent = _parent;
            localAddr = ipAddr;
            localAddrMask = ipv4mask;

            Array33<byte> ssidName = new();

            Encoding.ASCII.GetBytes("12345678123456781234567812345678").CopyTo(ssidName.AsSpan());
            FakeSsid = new Ssid
            {
                Length = 32,
                Name = ssidName
            };

            protocol = new LanProtocol(this);
            protocol.Accept += OnConnect;
            protocol.SyncNetwork += OnSyncNetwork;
            protocol.DisconnectStation += DisconnectStation;

            // FIXME: Quick Workaround
            networkInfo.Ldn.NodeCountMax = LanProtocol.NodeCountMax;
            networkInfo.Common.MacAddress = new Array6<byte>();
            networkInfo.Common.Ssid.Name = new Array33<byte>();
            networkInfo.Common.Ssid.Name.AsSpan().Fill(0);
            networkInfo.NetworkId.SessionId = new Array16<byte>();
            networkInfo.NetworkId.SessionId.AsSpan().Fill(0);
            networkInfo.Ldn.SecurityParameter = new Array16<byte>();
            networkInfo.Ldn.SecurityParameter.AsSpan().Fill(0);
            networkInfo.Ldn.Nodes = new Array8<NodeInfo>();
            for (int i = 0; i < LanProtocol.NodeCountMax; i++)
            {
                networkInfo.Ldn.Nodes[i] = new NodeInfo()
                {
                    MacAddress = new Array6<byte>(),
                    UserName = new Array33<byte>(),
                    Reserved2 = new Array16<byte>()
                };
                networkInfo.Ldn.Nodes[i].UserName.AsSpan().Fill(0);
                networkInfo.Ldn.Nodes[i].Reserved2.AsSpan().Fill(0);
            }

            networkInfo.Ldn.AdvertiseData = new Array384<byte>();
            networkInfo.Ldn.AdvertiseData.AsSpan().Fill(0);
            networkInfo.Ldn.Reserved4 = new Array140<byte>();
            networkInfo.Ldn.Reserved4.AsSpan().Fill(0);

            Initialize(listening);

            LogMsg("LanDiscovery init done.");
            // Linux: IUserCommService.GetIpv4Address() spamming messages while joining a lobby
        }

        public void Initialize(bool listening)
        {
            // this->stations array: add discovery and nodeInfo ref
            ResetStations();

            // Assign lanEvent func -> just log state

            if (!InitUdp(listening))
            {
                Logger.Info?.PrintMsg(LogClass.ServiceLdn, "LanDiscovery Initialize: InitUdp failed.");
                return;
            }

            // Create Worker Thread

            //SetCommState(NetworkState.Initialized);

            inited = true;
        }

        protected static IPAddress UintToIPAddress(uint address)
        {
            return new IPAddress(new byte[] {
                (byte)((address>>24) & 0xFF) ,
                (byte)((address>>16) & 0xFF) ,
                (byte)((address>>8)  & 0xFF) ,
                (byte)( address & 0xFF)});
        }

        protected void OnSyncNetwork(NetworkInfo info)
        {

            if (!networkInfo.Equals(info))
            {
                networkInfo = info;

                LogMsg($"OnSyncNetwork: Received NetworkInfo:\n{JsonHelper.Serialize(info, true)}");
                LogMsg($"OnSyncNetwork: hostIP: {UintToIPAddress(info.Ldn.Nodes[0].Ipv4Address)}");
                //if (commState == NetworkState.Station)
                //{
                //    SetCommState(NetworkState.StationConnected);
                //}
                // OnNetworkInfoChanged();

                parent.HandleSyncNetwork(info);
            }
        }

        protected void OnConnect(LdnProxyTcpSession station)
        {
            LogMsg("LanDiscovery OnConnect");

            if (stations.Count > LanProtocol.StationCountMax)
            {
                station.Disconnect();
                station.Dispose();
                return;
            }

            stations.Add(station);
            station.SetNodeId(stations.Count + 1);

            UpdateNodes();
        }

        public void DisconnectStation(LdnProxyTcpSession station)
        {
            if (!station.IsDisposed)
            {
                if (station.IsConnected)
                {
                    station.Disconnect();
                }
                station.Dispose();
            }
            networkInfo.Ldn.Nodes[stations.IndexOf(station)] = new NodeInfo()
            {
                MacAddress = new Array6<byte>(),
                UserName = new Array33<byte>(),
                Reserved2 = new Array16<byte>()
            };
            stations.Remove(station);

            UpdateNodes();
        }

        public bool SetAdvertiseData(byte[] data)
        {
            LogMsg("LanDiscovery SetAdvertiseData");

            if (data.Length > (int)LanProtocol.AdvertiseDataSizeMax)
            {
                return false;
            }

            Array384<byte> advertiseData = new();

            if (data.Length > 0)
            {
                data.CopyTo(advertiseData.AsSpan());
            }

            if (networkInfo.Ldn.AdvertiseData.Equals(advertiseData))
            {
                return true;
            }

            networkInfo.Ldn.AdvertiseData = advertiseData;
            networkInfo.Ldn.AdvertiseDataSize = (ushort)data.Length;

            LogMsg($"LanDiscovery SetAdvertiseData done: {BitConverter.ToString(advertiseData.AsSpan().ToArray())}");

            LogMsg($"LanDiscovery SetAdvertiseData NetworkInfo:\n{JsonHelper.Serialize(networkInfo, true)}");

            // results in SessionKeepFailed or MasterDisconnected
            if (networkInfo.Ldn.Nodes[0].IsConnected == 1)
            {
                UpdateNodes();
            }

            return true;
        }

        public bool InitNetworkInfo()
        {
            LogMsg("LanDiscovery InitNetworkInfo");

            if (!GetFakeMac(out networkInfo.Common.MacAddress))
            {
                return false;
            }
            networkInfo.Common.Channel = 6;
            networkInfo.Common.LinkLevel = 3;
            networkInfo.Common.NetworkType = 2;
            networkInfo.Common.Ssid = FakeSsid;

            networkInfo.Ldn.Nodes = new Array8<NodeInfo>();
            // TODO: check new stuff
            //if (networkInfo.Ldn.Nodes == null)
            //{
                // moved above
            //}

            for (int i = 0; i < LanProtocol.NodeCountMax; i++)
            {
                networkInfo.Ldn.Nodes[i].NodeId = (byte)i;
                networkInfo.Ldn.Nodes[i].IsConnected = 0;
            }

            return true;
        }

        protected bool GetFakeMac(out Array6<byte> macAddress, IPAddress address = null)
        {
            LogMsg("LanDiscovery GetFakeMac");

            if (address == null)
            {
                address = localAddr;
            }

            byte[] ip = address.GetAddressBytes();
            macAddress = new Array6<byte>();
            new byte[] { 0x02, 0x00, ip[0], ip[1], ip[2], ip[3] }.CopyTo(macAddress.AsSpan());
            return true;
        }

        public bool InitTcp(bool listening, IPAddress address = null, int port = DefaultPort)
        {
            LogMsg($"LanDiscovery InitTcp [listening: {listening}] [address: {address}]");

            if (tcp != null)
            {
                tcp.DisconnectAndStop();
                tcp.Dispose();
                tcp = null;
            }

            ILdnTcpSocket s;

            if (listening)
            {
                try
                {
                    if (address == null)
                    {
                        address = localAddr;
                    }
                    s = new LdnProxyTcpServer(protocol, address, port);
                }
                catch (Exception ex)
                {
                    Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"LanDiscovery encountered an error while setting up LdnProxyTcpServer: {ex}");
                    return false;
                }

                if (!s.Start())
                {
                    return false;
                }
            }
            else
            {
                if (address == null)
                {
                    return false;
                }

                try
                {
                    s = new LdnProxyTcpClient(protocol, address, port);
                }
                catch (Exception ex)
                {
                    Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"LanDiscovery encountered an error while setting up LdnProxyTcpClient: {ex}");
                    return false;
                }
            }

            tcp = s;

            return true;
        }

        public bool InitUdp(bool listening)
        {
            LogMsg("LanDiscovery InitUdp");

            if (udp != null)
            {
                udp.Stop();
            }

            if (listening)
            {
                try
                {
                    LdnProxyUdpServer tempudp = new LdnProxyUdpServer(protocol, localAddr, DefaultPort);
                    udp = tempudp;
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"LanDiscovery encountered an error while setting up LdnProxyUdpServer: {ex}");
                    return false;
                }
            }

            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LanDiscovery was not able to setup a udp client socket.");
            return false;
        }

        public NetworkInfo[] Scan(ushort channel, ScanFilter filter)
        {
            LogMsg("LanDiscovery Scan");

            int len = protocol.SendBroadcast(udp, LanPacketType.Scan, DefaultPort);
            if (len < 0)
            {
                return Array.Empty<NetworkInfo>();
            }

            // Sleep for 1 sec
            Thread.Sleep(1000);

            List<NetworkInfo> outNetworkInfo = new List<NetworkInfo>();

            foreach (KeyValuePair<Array6<byte>, NetworkInfo> item in udp.scanResults)
            {
                bool copy = true;
                if ((filter.Flag & ScanFilterFlag.LocalCommunicationId) > 0)
                {
                    copy &= filter.NetworkId.IntentId.LocalCommunicationId == item.Value.NetworkId.IntentId.LocalCommunicationId;
                }
                if ((filter.Flag & ScanFilterFlag.SessionId) > 0)
                {
                    copy &= filter.NetworkId.SessionId.AsSpan() == item.Value.NetworkId.SessionId.AsSpan();
                }
                if ((filter.Flag & ScanFilterFlag.NetworkType) > 0)
                {
                    // Why are these different types? NetworkInfo.Common.NetworkType should also be a NetworkType
                    copy &= filter.NetworkType == (NetworkType)item.Value.Common.NetworkType;
                }
                if ((filter.Flag & ScanFilterFlag.Ssid) > 0)
                {
                    // TODO: Check if this works
                    Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LanDiscovery ScanFilterFlag.Ssid hit. filter.Ssid: {filter.Ssid} NetworkInfo.Common.Ssid: {item.Value.Common.Ssid}");
                    copy &= filter.Ssid.Equals(item.Value.Common.Ssid);
                }
                if ((filter.Flag & ScanFilterFlag.SceneId) > 0)
                {
                    copy &= filter.NetworkId.IntentId.SceneId == item.Value.NetworkId.IntentId.SceneId;
                }

                if (copy)
                {
                    // TODO: maybe remove this userstring stuff
                    string userstring = "";
                    foreach (byte byte_char in item.Value.Ldn.Nodes[0].UserName.AsSpan())
                    {
                        userstring += byte_char.ToString();
                    }
                    if (userstring.CompareTo("000000000000000000000000000000000") != 0)
                    {
                        Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LanDiscovery Scan: Adding new NetworkInfo to list: {userstring}");
                        outNetworkInfo.Add(item.Value);
                    }
                    else
                    {
                        Logger.Warning?.PrintMsg(LogClass.ServiceLdn, $"LanDiscovery Scan: Got empty userstring. There might be a timing issue somewhere...");
                    }
                }
            }

            return outNetworkInfo.ToArray();
        }

        protected void ResetStations()
        {
            LogMsg("LanDiscovery ResetStations");

            foreach (LdnProxyTcpSession station in stations)
            {
                station.Disconnect();
                station.Dispose();
            }
            stations.Clear();
        }

        protected void UpdateNodes()
        {
            LogMsg("LanDiscovery UpdateNodes");

            int countConnected = 0;
            foreach (LdnProxyTcpSession station in stations)
            {
                if (station.IsConnected)
                {
                    countConnected++;
                    station.OverrideInfo();
                    // TODO: this is not part of the original impl - check if this makes things work
                    networkInfo.Ldn.Nodes[station.GetNodeId() - 1] = station.GetNodeInfo();
                }
            }
            byte nodeCount = (byte)(countConnected + 1);

            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LanDiscovery UpdateNodes: NetworkInfoNodeCount: {networkInfo.Ldn.NodeCount} | new NodeCount: {nodeCount}");
            bool networkInfoChanged = networkInfo.Ldn.NodeCount != nodeCount;

            networkInfo.Ldn.NodeCount = nodeCount;

            foreach (LdnProxyTcpSession station in stations)
            {
                if (station.IsConnected)
                {
                    if (protocol.SendPacket(station, LanPacketType.SyncNetwork, LdnHelper.StructureToByteArray(networkInfo)) < 0)
                    {
                        Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LanDiscovery UpdateNodes: Failed to send {LanPacketType.SyncNetwork} to station {station.GetNodeId()}");
                    }
                }
            }

            if (networkInfoChanged)
            {
                parent.HandleUpdateNodes(networkInfo);
            }
        }

        protected NodeInfo GetNodeInfo(NodeInfo node, UserConfig userConfig, ushort localCommunicationVersion)
        {
            LogMsg("LanDiscovery GetNodeInfo");

            uint ipAddress = NetworkHelpers.ConvertIpv4Address(localAddr);

            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LanDiscovery GetNodeInfo: addressInformation obtained. Address: {localAddr}");

            // !GetFakeMac() -> return bad result code
            if (GetFakeMac(out Array6<byte> macAddress, localAddr))
            {
                node.MacAddress = macAddress;
            }
            node.IsConnected = 1;
            node.UserName = userConfig.UserName;
            node.LocalCommunicationVersion = localCommunicationVersion;
            node.Ipv4Address = ipAddress;

            return node;
        }

        public bool CreateNetwork(SecurityConfig securityConfig, UserConfig userConfig, NetworkConfig networkConfig)
        {
            LogMsg("LanDiscovery CreateNetwork");

            //if (parent.commService.state != NetworkState.AccessPoint)
            //{
            //    return false;
            //}

            // This wouldn't be called otherwise
            //commState = NetworkState.AccessPoint;

            if (!InitTcp(true))
            {
                return false;
            }

            if (!InitNetworkInfo())
            {
                return false;
            }
            networkInfo.Ldn.NodeCountMax = networkConfig.NodeCountMax;
            networkInfo.Ldn.SecurityMode = (ushort)Convert.ChangeType(securityConfig.SecurityMode, Enum.GetUnderlyingType(typeof(SecurityMode)));

            if (networkConfig.Channel == 0)
            {
                networkInfo.Common.Channel = 6;
            }
            else
            {
                networkInfo.Common.Channel = networkConfig.Channel;
            }

            networkInfo.NetworkId.SessionId = new Array16<byte>();
            new Random().NextBytes(networkInfo.NetworkId.SessionId.AsSpan());
            networkInfo.NetworkId.IntentId = networkConfig.IntentId;

            networkInfo.Ldn.Nodes[0] = GetNodeInfo(networkInfo.Ldn.Nodes[0], userConfig, networkConfig.LocalCommunicationVersion);
            networkInfo.Ldn.Nodes[0].IsConnected = 1;
            networkInfo.Ldn.NodeCount++;

            //SetCommState(NetworkState.AccessPointCreated);

            parent.HandleCreateNetwork(networkInfo);

            //UpdateNodes();

            return true;
        }

        public void DestroyNetwork()
        {
            if (tcp != null)
            {
                try
                {
                    tcp.DisconnectAndStop();
                }
                finally
                {
                    tcp.Dispose();
                    tcp = null;
                }
            }
            ResetStations();

            // TODO: dasdasdasdasd
            //parent.HandleDisconnectNetwork(default, DisconnectReason.DisconnectedBySystem);

            // SetCommState(NetworkState.AccessPoint);
        }

        public NetworkError Connect(NetworkInfo networkInfo, UserConfig userConfig, uint localCommunicationVersion)
        {
            if (networkInfo.Ldn.NodeCount == 0)
            {
                return NetworkError.Unknown;
            }

            uint hostIp = networkInfo.Ldn.Nodes[0].Ipv4Address;
            LogMsg($"Connect: Got hostIP: {hostIp:X8}");

            IPAddress address = UintToIPAddress(hostIp);
            LogMsg($"Connecting to host: {address}");

            if (!InitTcp(false, address))
            {
                LogMsg("ConnectNotFound");
                return NetworkError.ConnectNotFound;
            }

            if (!tcp.ConnectAsync())
            {
                LogMsg("Failed to connect.");
                return NetworkError.ConnectFailure;
            }

            NodeInfo myNode = new NodeInfo();
            myNode = GetNodeInfo(myNode, userConfig, (ushort)localCommunicationVersion);
            int ret = protocol.SendPacket(tcp, LanPacketType.Connect, LdnHelper.StructureToByteArray(myNode));
            if (ret < 0)
            {
                return NetworkError.Unknown;
            }
            //InitNodeStateChange();

            parent.HandleConnected(networkInfo);

            // TODO: check if you could change anything here...

            // Sleep for 1 sec
            Thread.Sleep(1000);

            return NetworkError.None;
        }

        public void Dispose()
        {
            if (inited)
            {
                // Wait for workerThread and Destroy() it

                DisconnectAndStop();
                ResetStations();
                inited = false;
            }

            protocol.Accept -= OnConnect;
            protocol.SyncNetwork -= OnSyncNetwork;
            protocol.DisconnectStation -= DisconnectStation;
        }

        public void DisconnectAndStop()
        {
            if (udp != null)
            {
                try
                {
                    udp.Stop();
                }
                finally
                {
                    udp.Dispose();
                    udp = null;
                }
            }
            if (tcp != null)
            {
                try
                {
                    tcp.DisconnectAndStop();
                }
                finally
                {
                    tcp.Dispose();
                    tcp = null;
                }
            }
        }
    }
}
