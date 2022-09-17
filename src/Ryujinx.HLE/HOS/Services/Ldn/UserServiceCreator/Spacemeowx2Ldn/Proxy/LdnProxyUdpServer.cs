using Ryujinx.Common.Logging;
using Ryujinx.Common.Memory;
using Ryujinx.HLE.HOS.Services.Ldn.Spacemeowx2Ldn;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Spacemeowx2Ldn.Proxy
{
    internal class LdnProxyUdpServer : NetCoreServer.UdpServer, ILdnSocket
    {
        private LanProtocol _protocol;
        private byte[] _buffer;
        private int _bufferEnd;

        internal Dictionary<ulong, NetworkInfo> scanResults = new Dictionary<ulong, NetworkInfo>();

        public LdnProxyUdpServer(LanProtocol protocol, IPAddress address, int port) : base(address, port)
        {
            _protocol = protocol;
            _protocol.Scan += HandleScan;
            _protocol.ScanResponse += HandleScanResponse;
            _buffer = new byte[LanProtocol.BufferSize];
            OptionReuseAddress = true;
            OptionReceiveBufferSize = LanProtocol.BufferSize;
            OptionSendBufferSize = LanProtocol.BufferSize;

            Start();
        }

        protected override Socket CreateSocket()
        {
            Socket socket = new Socket(Endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
            {
                EnableBroadcast = true
            };

            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);

            return socket;
        }

        protected override void OnStarted()
        {
            ReceiveAsync();
        }

        protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
        {
            _protocol.Read(ref _buffer, ref _bufferEnd, buffer, (int)offset, (int)size, endpoint);
            ReceiveAsync();
        }

        protected override void OnError(SocketError error)
        {
            Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"LdnProxyUdpServer caught an error with code {error}");
        }

        protected override void Dispose(bool disposingManagedResources)
        {
            _protocol.Scan -= HandleScan;
            _protocol.ScanResponse -= HandleScanResponse;
            base.Dispose(disposingManagedResources);
        }

        public bool SendPacketAsync(EndPoint endpoint, byte[] data)
        {
            return SendAsync(endpoint, data);
        }

        private void HandleScan(EndPoint endpoint, LanPacketType type, byte[] data)
        {
            _protocol.SendPacket(this, type, data, endpoint);
        }

        private void HandleScanResponse(NetworkInfo info)
        {
            scanResults.Add(BitConverter.ToUInt64(info.Common.MacAddress.AsSpan()), info);
        }
    }
}
