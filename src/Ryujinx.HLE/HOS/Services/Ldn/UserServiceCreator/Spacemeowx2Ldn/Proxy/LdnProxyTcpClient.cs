using Ryujinx.Common.Logging;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Spacemeowx2Ldn.Proxy
{
    internal class LdnProxyTcpClient : NetCoreServer.TcpClient, ILdnTcpSocket
    {
        private LanProtocol _protocol;
        private IPAddress _serverAddress;
        private byte[] _buffer;
        private int _bufferEnd;

        public LdnProxyTcpClient(LanProtocol protocol, IPAddress address, int port) : base(address, port)
        {
            OptionReceiveBufferSize = LanProtocol.BufferSize;
            OptionSendBufferSize = LanProtocol.BufferSize;
            OptionNoDelay = true;
            _serverAddress = address;
            _protocol = protocol;
            _buffer = new byte[LanProtocol.BufferSize];
        }

        protected override void OnConnected()
        {
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LdnProxyTCPClient connected!");
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            _protocol.Read(ref _buffer, ref _bufferEnd, buffer, (int)offset, (int)size);
        }

        public void DisconnectAndStop()
        {
            DisconnectAsync();
            while (IsConnected)
            {
                Thread.Yield();
            }
        }

        public bool SendPacketAsync(EndPoint endPoint, byte[] data)
        {
            if (endPoint != null)
            {
                Logger.Warning?.PrintMsg(LogClass.ServiceLdn, $"LdnProxyTcpClient is sending a packet but endpoint is not null.");
            }

            if (IsConnecting && !IsConnected)
            {
                Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LdnProxyTCPClient needs to connect before sending packets. Waiting...");
                while (IsConnecting && !IsConnected)
                {
                    Thread.Yield();
                }
            }

            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"Sending packet to: {_serverAddress}");

            return SendAsync(data);
        }

        protected override void OnError(SocketError error)
        {
            Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"LdnProxyTCPClient caught an error with code {error}");
        }

        protected override void Dispose(bool disposingManagedResources)
        {
            DisconnectAndStop();
            base.Dispose(disposingManagedResources);
        }

        public bool Start()
        {
            Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"LdnProxyTCPClient Start was called.");
            return false;
        }

        public bool Stop()
        {
            Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"LdnProxyTCPClient Stop was called.");
            return false;
        }
    }
}
