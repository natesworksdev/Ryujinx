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
        private LdnProxyUdpServer _udp;
        private List<LdnProxyTcpSession> _stations = new List<LdnProxyTcpSession>();

        internal readonly IPAddress LocalAddr;
        internal readonly IPAddress LocalBroadcastAddr;
        internal NetworkInfo NetworkInfo;

        // NOTE: Credit to https://stackoverflow.com/a/39338188
        private static IPAddress GetBroadcastAddress(IPAddress address, IPAddress mask)
        {
            uint ipAddress = BitConverter.ToUInt32(address.GetAddressBytes(), 0);
            uint ipMaskV4 = BitConverter.ToUInt32(mask.GetAddressBytes(), 0);
            uint broadCastIpAddress = ipAddress | ~ipMaskV4;

            return new IPAddress(BitConverter.GetBytes(broadCastIpAddress));
        }

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

        public LanDiscovery(Spacemeowx2LdnClient parent, IPAddress ipAddress, IPAddress ipv4mask)
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"Initialize LanDiscovery using IP: {ipAddress}");

            _parent = parent;
            LocalAddr = ipAddress;
            LocalBroadcastAddr = GetBroadcastAddress(ipAddress, ipv4mask);

            _fakeSsid = new()
            {
                Length = (byte)LanProtocol.SsidLengthMax,
            };
            Encoding.ASCII.GetBytes("12345678123456781234567812345678").CopyTo(_fakeSsid.Name.AsSpan());

            _protocol = new LanProtocol(this);
            _protocol.Accept += OnConnect;
            _protocol.SyncNetwork += OnSyncNetwork;
            _protocol.DisconnectStation += DisconnectStation;

            NetworkInfo = GetEmptyNetworkInfo();

            ResetStations();

            if (!InitUdp())
            {
                Logger.Error?.PrintMsg(LogClass.ServiceLdn, "LanDiscovery Initialize: InitUdp failed.");

                return;
            }

            _initialized = true;
        }

        protected void OnSyncNetwork(NetworkInfo info)
        {
            if (!NetworkInfo.Equals(info))
            {
                NetworkInfo = info;

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

            station.NodeId = _stations.Count + 1;

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

            NetworkInfo.Ldn.Nodes[_stations.IndexOf(station)] = new NodeInfo()
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

            data.CopyTo(NetworkInfo.Ldn.AdvertiseData.AsSpan());
            NetworkInfo.Ldn.AdvertiseDataSize = (ushort)data.Length;
            Logger.Debug?.PrintMsg(LogClass.ServiceLdn, $"AdvertiseData: {BitConverter.ToString(data)}");
            // Logger.Debug?.PrintMsg(LogClass.ServiceLdn, $"NetworkInfo:\n{JsonHelper.Serialize(NetworkInfo, true)}");

            // NOTE: Otherwise this results in SessionKeepFailed or MasterDisconnected
            if (NetworkInfo.Ldn.Nodes[0].IsConnected == 1)
            {
                UpdateNodes();
            }

            return true;
        }

        public void InitNetworkInfo()
        {
            NetworkInfo.Common.MacAddress = GetFakeMac();
            NetworkInfo.Common.Channel = COMMON_CHANNEL;
            NetworkInfo.Common.LinkLevel = COMMON_LINK_LEVEL;
            NetworkInfo.Common.NetworkType = COMMON_NETWORK_TYPE;
            NetworkInfo.Common.Ssid = _fakeSsid;

            NetworkInfo.Ldn.Nodes = new Array8<NodeInfo>();

            for (int i = 0; i < LanProtocol.NodeCountMax; i++)
            {
                NetworkInfo.Ldn.Nodes[i].NodeId = (byte)i;
                NetworkInfo.Ldn.Nodes[i].IsConnected = 0;
            }
        }

        protected Array6<byte> GetFakeMac(IPAddress address = null)
        {
            if (address == null)
            {
                address = LocalAddr;
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
                        address = LocalAddr;
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

        public bool InitUdp()
        {
            if (_udp != null)
            {
                _udp.Stop();
            }

            try
            {
                // NOTE: Linux won't receive any broadcast packets if the socket is not bound to the broadcast address.
                //       Windows only works if bound to localhost or the local address.
                //       See this discussion: https://stackoverflow.com/questions/13666789/receiving-udp-broadcast-packets-on-linux
                _udp = new LdnProxyUdpServer(_protocol, OperatingSystem.IsLinux() ? LocalBroadcastAddr : LocalAddr, DEFAULT_PORT);
            }
            catch (Exception ex)
            {
                Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"Failed to create LdnProxyUdpServer: {ex}");

                return false;
            }

            return true;
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

                if (filter.Flag.HasFlag(ScanFilterFlag.LocalCommunicationId))
                {
                    copy &= filter.NetworkId.IntentId.LocalCommunicationId == item.Value.NetworkId.IntentId.LocalCommunicationId;
                }

                if (filter.Flag.HasFlag(ScanFilterFlag.SessionId))
                {
                    copy &= filter.NetworkId.SessionId.AsSpan() == item.Value.NetworkId.SessionId.AsSpan();
                }

                if (filter.Flag.HasFlag(ScanFilterFlag.NetworkType))
                {
                    copy &= filter.NetworkType == (NetworkType)item.Value.Common.NetworkType;
                }

                if (filter.Flag.HasFlag(ScanFilterFlag.Ssid))
                {
                    copy &= filter.Ssid.Equals(item.Value.Common.Ssid);
                }

                if (filter.Flag.HasFlag(ScanFilterFlag.SceneId))
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
                        Logger.Warning?.PrintMsg(LogClass.ServiceLdn, $"LanDiscovery Scan: Got empty Username. There might be a timing issue somewhere...");
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
                    NetworkInfo.Ldn.Nodes[station.NodeId - 1] = station.NodeInfo;
                }
            }

            byte nodeCount = (byte)(countConnected + 1);

            Logger.Debug?.PrintMsg(LogClass.ServiceLdn, $"NetworkInfoNodeCount: {NetworkInfo.Ldn.NodeCount} | new NodeCount: {nodeCount}");

            bool networkInfoChanged = NetworkInfo.Ldn.NodeCount != nodeCount;

            NetworkInfo.Ldn.NodeCount = nodeCount;

            foreach (LdnProxyTcpSession station in _stations)
            {
                if (station.IsConnected)
                {
                    if (_protocol.SendPacket(station, LanPacketType.SyncNetwork, LdnHelper.StructureToByteArray(NetworkInfo)) < 0)
                    {
                        Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"Failed to send {LanPacketType.SyncNetwork} to station {station.NodeId}");
                    }
                }
            }

            if (networkInfoChanged)
            {
                _parent.InvokeNetworkChange(NetworkInfo, true);
            }
        }

        protected NodeInfo GetNodeInfo(NodeInfo node, UserConfig userConfig, ushort localCommunicationVersion)
        {
            uint ipAddress = NetworkHelpers.ConvertIpv4Address(LocalAddr);

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

            NetworkInfo.Ldn.NodeCountMax = networkConfig.NodeCountMax;
            NetworkInfo.Ldn.SecurityMode = (ushort)securityConfig.SecurityMode;

            if (networkConfig.Channel == 0)
            {
                NetworkInfo.Common.Channel = 6;
            }
            else
            {
                NetworkInfo.Common.Channel = networkConfig.Channel;
            }

            NetworkInfo.NetworkId.SessionId = new Array16<byte>();
            new Random().NextBytes(NetworkInfo.NetworkId.SessionId.AsSpan());
            NetworkInfo.NetworkId.IntentId = networkConfig.IntentId;

            NetworkInfo.Ldn.Nodes[0] = GetNodeInfo(NetworkInfo.Ldn.Nodes[0], userConfig, networkConfig.LocalCommunicationVersion);
            NetworkInfo.Ldn.Nodes[0].IsConnected = 1;
            NetworkInfo.Ldn.NodeCount++;

            _parent.InvokeNetworkChange(NetworkInfo, true);

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
