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
        private const ushort COMMON_CHANNEL = 6;
        private const byte COMMON_LINK_LEVEL = 3;
        private const byte COMMON_NETWORK_TYPE = 2;

        private Spacemeowx2LdnClient _parent;
        private LanProtocol _protocol;
        private bool _initialized;
        private readonly Ssid _fakeSsid;
        private ILdnTcpSocket _tcp;
        // NOTE: This type may need to be changed to ILdnUdpSocket in the future
        private LdnProxyUdpServer _udp;
        private List<LdnProxyTcpSession> _stations = new List<LdnProxyTcpSession>();

        internal readonly IPAddress localAddr;
        internal readonly IPAddress localAddrMask;
        internal NetworkInfo networkInfo;

        private static NetworkInfo GetEmptyNetworkInfo()
        {
            NetworkInfo networkInfo = new NetworkInfo()
            {
                NetworkId = new()
                {
                    SessionId = new Array16<byte>()
                },
                Common = new()
                {
                    MacAddress = new Array6<byte>(),
                    Ssid = new()
                    {
                        Name = new Array33<byte>()
                    }
                },
                Ldn = new()
                {
                    NodeCountMax = LanProtocol.NodeCountMax,
                    SecurityParameter = new Array16<byte>(),
                    Nodes = new Array8<NodeInfo>(),
                    AdvertiseData = new Array384<byte>(),
                    Reserved4 = new Array140<byte>()
                }
            };

            for (int i = 0; i < LanProtocol.NodeCountMax; i++)
            {
                networkInfo.Ldn.Nodes[i] = new()
                {
                    MacAddress = new Array6<byte>(),
                    UserName = new Array33<byte>(),
                    Reserved2 = new Array16<byte>()
                };
            }

            return networkInfo;
        }

        public LanDiscovery(Spacemeowx2LdnClient parent, IPAddress ipAddress, IPAddress ipv4mask, bool listening = true)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"Initialize LanDiscovery using IP: {ipAddress}");

            _parent = parent;
            localAddr = ipAddress;
            localAddrMask = ipv4mask;

            _fakeSsid = new()
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
        }

        public void Initialize(bool listening)
        {
            ResetStations();

            if (!InitUdp(listening))
            {
                Logger.Error?.PrintMsg(LogClass.ServiceLdn, "LanDiscovery Initialize: InitUdp failed.");

                return;
            }

            _initialized = true;
        }

        protected void OnSyncNetwork(NetworkInfo info)
        {
            if (!networkInfo.Equals(info))
            {
                networkInfo = info;

                // Logger.Debug?.PrintMsg(LogClass.ServiceLdn, $"Received NetworkInfo:\n{JsonHelper.Serialize(info, true)}");
                Logger.Debug?.PrintMsg(LogClass.ServiceLdn, $"Host IP: {NetworkHelpers.ConvertUint(info.Ldn.Nodes[0].Ipv4Address)}");

                _parent.InvokeNetworkChange(info, true);
            }
        }

        protected void OnConnect(LdnProxyTcpSession station)
        {
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

            if (data.Length > (int)LanProtocol.AdvertiseDataSizeMax)
            {
                Logger.Error?.PrintMsg(LogClass.ServiceLdn, "AdvertiseData exceeds size limit.");

                return false;
            }

            data.CopyTo(networkInfo.Ldn.AdvertiseData.AsSpan());
            networkInfo.Ldn.AdvertiseDataSize = (ushort)data.Length;

            Logger.Debug?.PrintMsg(LogClass.ServiceLdn, $"AdvertiseData: {BitConverter.ToString(data)}");
            // Logger.Debug?.PrintMsg(LogClass.ServiceLdn, $"NetworkInfo:\n{JsonHelper.Serialize(networkInfo, true)}");

            // NOTE: Otherwise this results in SessionKeepFailed or MasterDisconnected
            if (networkInfo.Ldn.Nodes[0].IsConnected == 1)
            {
                UpdateNodes();
            }

            return true;
        }

        public void InitNetworkInfo()
        {
            networkInfo.Common.MacAddress = GetFakeMac();
            networkInfo.Common.Channel = COMMON_CHANNEL;
            networkInfo.Common.LinkLevel = COMMON_LINK_LEVEL;
            networkInfo.Common.NetworkType = COMMON_NETWORK_TYPE;
            networkInfo.Common.Ssid = _fakeSsid;

            networkInfo.Ldn.Nodes = new Array8<NodeInfo>();

            for (int i = 0; i < LanProtocol.NodeCountMax; i++)
            {
                networkInfo.Ldn.Nodes[i].NodeId = (byte)i;
                networkInfo.Ldn.Nodes[i].IsConnected = 0;
            }
        }

        protected Array6<byte> GetFakeMac(IPAddress address = null)
        {
            if (address == null)
            {
                address = localAddr;
            }

            byte[] ip = address.GetAddressBytes();

            var macAddress = new Array6<byte>();
            new byte[] { 0x02, 0x00, ip[0], ip[1], ip[2], ip[3] }.CopyTo(macAddress.AsSpan());

            return macAddress;
        }

        public bool InitTcp(bool listening, IPAddress address = null, int port = DEFAULT_PORT)
        {
            Logger.Debug?.PrintMsg(LogClass.ServiceLdn, $"LanDiscovery InitTcp: IP: {address}, listening: {listening}");

            if (_tcp != null)
            {
                _tcp.DisconnectAndStop();
                _tcp.Dispose();
                _tcp = null;
            }

            ILdnTcpSocket tcpSocket;

            if (listening)
            {
                try
                {
                    if (address == null)
                    {
                        address = localAddr;
                    }

                    tcpSocket = new LdnProxyTcpServer(_protocol, address, port);
                }
                catch (Exception ex)
                {
                    Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"Failed to create LdnProxyTcpServer: {ex}");

                    return false;
                }

                if (!tcpSocket.Start())
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
                    tcpSocket = new LdnProxyTcpClient(_protocol, address, port);
                }
                catch (Exception ex)
                {
                    Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"Failed to create LdnProxyTcpClient: {ex}");

                    return false;
                }
            }

            _tcp = tcpSocket;

            return true;
        }

        public bool InitUdp(bool listening)
        {
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
                    Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"Failed to create LdnProxyUdpServer: {ex}");

                    return false;
                }
            }

            Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"Failed to create a udp client socket.");

            return false;
        }

        public NetworkInfo[] Scan(ushort channel, ScanFilter filter)
        {
            if (_protocol.SendBroadcast(_udp, LanPacketType.Scan, DEFAULT_PORT) < 0)
            {
                return Array.Empty<NetworkInfo>();
            }

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
            foreach (LdnProxyTcpSession station in _stations)
            {
                station.Disconnect();
                station.Dispose();
            }
            _stations.Clear();
        }

        protected void UpdateNodes()
        {
            int countConnected = 0;

            foreach (LdnProxyTcpSession station in _stations)
            {
                if (station.IsConnected)
                {
                    countConnected++;
                    station.OverrideInfo();
                    // NOTE: This is not part of the original implementation.
                    networkInfo.Ldn.Nodes[station.nodeId - 1] = station.nodeInfo;
                }
            }
            byte nodeCount = (byte)(countConnected + 1);

            Logger.Debug?.PrintMsg(LogClass.ServiceLdn, $"NetworkInfoNodeCount: {networkInfo.Ldn.NodeCount} | new NodeCount: {nodeCount}");

            bool networkInfoChanged = networkInfo.Ldn.NodeCount != nodeCount;

            networkInfo.Ldn.NodeCount = nodeCount;

            foreach (LdnProxyTcpSession station in _stations)
            {
                if (station.IsConnected)
                {
                    if (_protocol.SendPacket(station, LanPacketType.SyncNetwork, LdnHelper.StructureToByteArray(networkInfo)) < 0)
                    {
                        Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"Failed to send {LanPacketType.SyncNetwork} to station {station.nodeId}");
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
            uint ipAddress = NetworkHelpers.ConvertIpv4Address(localAddr);

            node.MacAddress = GetFakeMac();
            node.IsConnected = 1;
            node.UserName = userConfig.UserName;
            node.LocalCommunicationVersion = localCommunicationVersion;
            node.Ipv4Address = ipAddress;

            return node;
        }

        public bool CreateNetwork(SecurityConfig securityConfig, UserConfig userConfig, NetworkConfig networkConfig)
        {
            if (!InitTcp(true))
            {
                return false;
            }

            InitNetworkInfo();

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
                Logger.Error?.PrintMsg(LogClass.ServiceLdn, "Failed to connect.");

                return NetworkError.ConnectFailure;
            }

            NodeInfo myNode = GetNodeInfo(new NodeInfo(), userConfig, (ushort)localCommunicationVersion);
            if (_protocol.SendPacket(_tcp, LanPacketType.Connect, LdnHelper.StructureToByteArray(myNode)) < 0)
            {
                return NetworkError.Unknown;
            }

            _parent.InvokeNetworkChange(networkInfo, true);

            Thread.Sleep(1000);

            return NetworkError.None;
        }

        public void Dispose()
        {
            if (_initialized)
            {
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
