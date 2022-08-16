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
    internal class LanDiscovery : IDisposable
    {
        private const int DEFAULT_PORT = 11452;

        private Spacemeowx2LdnClient _parent;
        private LanProtocol _protocol;
        private bool _initialized;
        private readonly Ssid _fakeSsid;
        private ILdnTcpSocket _tcp;
        // NOTE: Type may need to be changed to ILdnUdpSocket in the future
        private LdnProxyUdpServer _udp;
        private List<LdnProxyTcpSession> _stations = new List<LdnProxyTcpSession>();

        internal readonly IPAddress localAddr;
        internal readonly IPAddress localAddrMask;
        internal NetworkInfo networkInfo;

        private static NetworkInfo GetEmptyNetworkInfo()
        {
            NetworkInfo networkInfo = new NetworkInfo()
            {
                NetworkId = {
                    SessionId = new Array16<byte>()
                },
                Common = {
                    MacAddress = new Array6<byte>(),
                    Ssid = {
                        Name = new Array33<byte>()
                    }
                },
                Ldn = {
                    NodeCountMax      = LanProtocol.NodeCountMax,
                    SecurityParameter = new Array16<byte>(),
                    Nodes             = new Array8<NodeInfo>(),
                    AdvertiseData     = new Array384<byte>(),
                    Reserved4         = new Array140<byte>()
                }
            };

            for (int i = 0; i < LanProtocol.NodeCountMax; i++)
            {
                networkInfo.Ldn.Nodes[i] = new NodeInfo()
                {
                    MacAddress = new Array6<byte>(),
                    UserName = new Array33<byte>(),
                    Reserved2 = new Array16<byte>()
                };
            }

            return networkInfo;
        }

        public LanDiscovery(Spacemeowx2LdnClient parent, IPAddress ipAddr, IPAddress ipv4mask, bool listening = true)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"Init LanDiscovery using IP: {ipAddr}");

            _parent = parent;
            localAddr = ipAddr;
            localAddrMask = ipv4mask;

            _fakeSsid = new Ssid
            {
                Length = (byte)LanProtocol.SsidLengthMax,
            };
            Encoding.ASCII.GetBytes("12345678123456781234567812345678").CopyTo(_fakeSsid.Name.AsSpan());

            _protocol = new LanProtocol(this);
            _protocol.Accept += OnConnect;
            _protocol.SyncNetwork += OnSyncNetwork;
            _protocol.DisconnectStation += DisconnectStation;

            networkInfo = LanDiscovery.GetEmptyNetworkInfo();
            Initialize(listening);

            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "LanDiscovery init done.");
        }

        public void Initialize(bool listening)
        {
            // this->stations array: add discovery and nodeInfo ref
            ResetStations();

            // Assign lanEvent func -> just log state

            if (!InitUdp(listening))
            {
                Logger.Error?.PrintMsg(LogClass.ServiceLdn, "LanDiscovery Initialize: InitUdp failed.");
                return;
            }

            // Create Worker Thread

            _initialized = true;
        }

        protected void OnSyncNetwork(NetworkInfo info)
        {
            if (!networkInfo.Equals(info))
            {
                networkInfo = info;

                // Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"OnSyncNetwork: Received NetworkInfo:\n{JsonHelper.Serialize(info, true)}");
                Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"OnSyncNetwork: hostIP: {NetworkHelpers.ConvertUint(info.Ldn.Nodes[0].Ipv4Address)}");

                _parent.InvokeNetworkChange(info, true);
            }
        }

        protected void OnConnect(LdnProxyTcpSession station)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "LanDiscovery OnConnect");

            if (_stations.Count > LanProtocol.StationCountMax)
            {
                station.Disconnect();
                station.Dispose();
                return;
            }

            _stations.Add(station);
            station.nodeId = _stations.Count + 1;

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
            networkInfo.Ldn.Nodes[_stations.IndexOf(station)] = new NodeInfo()
            {
                MacAddress = new Array6<byte>(),
                UserName = new Array33<byte>(),
                Reserved2 = new Array16<byte>()
            };
            _stations.Remove(station);

            UpdateNodes();
        }

        public bool SetAdvertiseData(byte[] data)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "LanDiscovery SetAdvertiseData");

            if (data.Length > (int)LanProtocol.AdvertiseDataSizeMax)
            {
                Logger.Error?.PrintMsg(LogClass.ServiceLdn, "AdvertiseData exceeds size limit.");
                return false;
            }

            data.CopyTo(networkInfo.Ldn.AdvertiseData.AsSpan());
            networkInfo.Ldn.AdvertiseDataSize = (ushort)data.Length;

            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LanDiscovery SetAdvertiseData done: {BitConverter.ToString(data)}");

            // Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LanDiscovery SetAdvertiseData NetworkInfo:\n{JsonHelper.Serialize(networkInfo, true)}");

            // Otherwise this results in SessionKeepFailed or MasterDisconnected
            if (networkInfo.Ldn.Nodes[0].IsConnected == 1)
            {
                UpdateNodes();
            }

            return true;
        }

        public bool InitNetworkInfo()
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "LanDiscovery InitNetworkInfo");

            if (!GetFakeMac(out networkInfo.Common.MacAddress))
            {
                return false;
            }
            networkInfo.Common.Channel = 6;
            networkInfo.Common.LinkLevel = 3;
            networkInfo.Common.NetworkType = 2;
            networkInfo.Common.Ssid = _fakeSsid;

            networkInfo.Ldn.Nodes = new Array8<NodeInfo>();

            for (int i = 0; i < LanProtocol.NodeCountMax; i++)
            {
                networkInfo.Ldn.Nodes[i].NodeId = (byte)i;
                networkInfo.Ldn.Nodes[i].IsConnected = 0;
            }

            return true;
        }

        protected bool GetFakeMac(out Array6<byte> macAddress, IPAddress address = null)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "LanDiscovery GetFakeMac");

            if (address == null)
            {
                address = localAddr;
            }

            byte[] ip = address.GetAddressBytes();
            macAddress = new Array6<byte>();
            new byte[] { 0x02, 0x00, ip[0], ip[1], ip[2], ip[3] }.CopyTo(macAddress.AsSpan());
            return true;
        }

        public bool InitTcp(bool listening, IPAddress address = null, int port = DEFAULT_PORT)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LanDiscovery InitTcp [listening: {listening}] [address: {address}]");

            if (_tcp != null)
            {
                _tcp.DisconnectAndStop();
                _tcp.Dispose();
                _tcp = null;
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
                    s = new LdnProxyTcpServer(_protocol, address, port);
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
                    s = new LdnProxyTcpClient(_protocol, address, port);
                }
                catch (Exception ex)
                {
                    Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"LanDiscovery encountered an error while setting up LdnProxyTcpClient: {ex}");
                    return false;
                }
            }

            _tcp = s;

            return true;
        }

        public bool InitUdp(bool listening)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "LanDiscovery InitUdp");

            if (_udp != null)
            {
                _udp.Stop();
            }

            if (listening)
            {
                try
                {
                    _udp = new LdnProxyUdpServer(_protocol, localAddr, DEFAULT_PORT);
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
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "LanDiscovery Scan");

            int len = _protocol.SendBroadcast(_udp, LanPacketType.Scan, DEFAULT_PORT);
            if (len < 0)
            {
                return Array.Empty<NetworkInfo>();
            }

            // Sleep for 1s
            Thread.Sleep(1000);

            List<NetworkInfo> outNetworkInfo = new List<NetworkInfo>();

            foreach (KeyValuePair<Array6<byte>, NetworkInfo> item in _udp.scanResults)
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
                    copy &= filter.NetworkType == (NetworkType)item.Value.Common.NetworkType;
                }
                if ((filter.Flag & ScanFilterFlag.Ssid) > 0)
                {
                    copy &= filter.Ssid.Equals(item.Value.Common.Ssid);
                }
                if ((filter.Flag & ScanFilterFlag.SceneId) > 0)
                {
                    copy &= filter.NetworkId.IntentId.SceneId == item.Value.NetworkId.IntentId.SceneId;
                }

                if (copy)
                {
                    if (item.Value.Ldn.Nodes[0].UserName[0] != 0)
                    {
                        Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LanDiscovery Scan: Adding NetworkInfo to list");
                        outNetworkInfo.Add(item.Value);
                    }
                    else
                    {
                        Logger.Warning?.PrintMsg(LogClass.ServiceLdn, $"LanDiscovery Scan: Got empty UserName. There might be a timing issue somewhere...");
                    }
                }
            }

            return outNetworkInfo.ToArray();
        }

        protected void ResetStations()
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "LanDiscovery ResetStations");

            foreach (LdnProxyTcpSession station in _stations)
            {
                station.Disconnect();
                station.Dispose();
            }
            _stations.Clear();
        }

        protected void UpdateNodes()
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "LanDiscovery UpdateNodes");

            int countConnected = 0;
            foreach (LdnProxyTcpSession station in _stations)
            {
                if (station.IsConnected)
                {
                    countConnected++;
                    station.OverrideInfo();
                    // NOTE: This is not part of the original implementation
                    networkInfo.Ldn.Nodes[station.nodeId - 1] = station.nodeInfo;
                }
            }
            byte nodeCount = (byte)(countConnected + 1);

            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LanDiscovery UpdateNodes: NetworkInfoNodeCount: {networkInfo.Ldn.NodeCount} | new NodeCount: {nodeCount}");
            bool networkInfoChanged = networkInfo.Ldn.NodeCount != nodeCount;

            networkInfo.Ldn.NodeCount = nodeCount;

            foreach (LdnProxyTcpSession station in _stations)
            {
                if (station.IsConnected)
                {
                    if (_protocol.SendPacket(station, LanPacketType.SyncNetwork, LdnHelper.StructureToByteArray(networkInfo)) < 0)
                    {
                        Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"LanDiscovery UpdateNodes: Failed to send {LanPacketType.SyncNetwork} to station {station.nodeId}");
                    }
                }
            }

            if (networkInfoChanged)
            {
                _parent.InvokeNetworkChange(networkInfo, true);
            }
        }

        protected NodeInfo GetNodeInfo(NodeInfo node, UserConfig userConfig, ushort localCommunicationVersion)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "LanDiscovery GetNodeInfo");

            uint ipAddress = NetworkHelpers.ConvertIpv4Address(localAddr);

            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LanDiscovery GetNodeInfo: addressInformation obtained. Address: {localAddr}");

            // !GetFakeMac() -> return bad result code
            if (GetFakeMac(out Array6<byte> macAddress, localAddr))
                node.MacAddress = macAddress;

            node.IsConnected = 1;
            node.UserName = userConfig.UserName;
            node.LocalCommunicationVersion = localCommunicationVersion;
            node.Ipv4Address = ipAddress;

            return node;
        }

        public bool CreateNetwork(SecurityConfig securityConfig, UserConfig userConfig, NetworkConfig networkConfig)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, "LanDiscovery CreateNetwork");

            if (!InitTcp(true) || !InitNetworkInfo())
            {
                return false;
            }

            networkInfo.Ldn.NodeCountMax = networkConfig.NodeCountMax;
            networkInfo.Ldn.SecurityMode = (ushort)securityConfig.SecurityMode;

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

            _parent.InvokeNetworkChange(networkInfo, true);

            return true;
        }

        public void DestroyNetwork()
        {
            if (_tcp != null)
            {
                try
                {
                    _tcp.DisconnectAndStop();
                }
                finally
                {
                    _tcp.Dispose();
                    _tcp = null;
                }
            }
            ResetStations();
        }

        public NetworkError Connect(NetworkInfo networkInfo, UserConfig userConfig, uint localCommunicationVersion)
        {
            if (networkInfo.Ldn.NodeCount == 0)
            {
                return NetworkError.Unknown;
            }

            IPAddress address = NetworkHelpers.ConvertUint(networkInfo.Ldn.Nodes[0].Ipv4Address);
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"Connecting to host: {address}");

            if (!InitTcp(false, address))
            {
                Logger.Error?.PrintMsg(LogClass.ServiceLdn, "Could not initialize TCPClient");
                return NetworkError.ConnectNotFound;
            }

            if (!_tcp.ConnectAsync())
            {
                Logger.Info?.PrintMsg(LogClass.ServiceLdn, "Failed to connect.");
                return NetworkError.ConnectFailure;
            }

            NodeInfo myNode = GetNodeInfo(new NodeInfo(), userConfig, (ushort)localCommunicationVersion);
            int ret = _protocol.SendPacket(_tcp, LanPacketType.Connect, LdnHelper.StructureToByteArray(myNode));
            if (ret < 0)
            {
                return NetworkError.Unknown;
            }

            _parent.InvokeNetworkChange(networkInfo, true);

            // Sleep for 1s
            Thread.Sleep(1000);

            return NetworkError.None;
        }

        public void Dispose()
        {
            if (_initialized)
            {
                // Wait for workerThread and Destroy() it

                DisconnectAndStop();
                ResetStations();
                _initialized = false;
            }

            _protocol.Accept -= OnConnect;
            _protocol.SyncNetwork -= OnSyncNetwork;
            _protocol.DisconnectStation -= DisconnectStation;
        }

        public void DisconnectAndStop()
        {
            if (_udp != null)
            {
                try
                {
                    _udp.Stop();
                }
                finally
                {
                    _udp.Dispose();
                    _udp = null;
                }
            }
            if (_tcp != null)
            {
                try
                {
                    _tcp.DisconnectAndStop();
                }
                finally
                {
                    _tcp.Dispose();
                    _tcp = null;
                }
            }
        }
    }
}
