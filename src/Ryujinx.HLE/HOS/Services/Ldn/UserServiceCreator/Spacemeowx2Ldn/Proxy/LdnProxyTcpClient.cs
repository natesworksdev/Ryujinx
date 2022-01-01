using Ryujinx.Common.Logging;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Spacemeowx2Ldn.Proxy
{
    class LdnProxyTcpClient : NetCoreServer.TcpClient, ILdnTcpSocket
    {
        //private bool _stop;
        private LanProtocol _protocol;

        private byte[] _buffer;
        private int _bufferEnd;
        private IPAddress _serverAddress;

        public LdnProxyTcpClient(LanProtocol protocol, IPAddress address, int port) :
            base(address, port)
        {
            OptionReceiveBufferSize = LanProtocol.BufferSize;
            OptionSendBufferSize = LanProtocol.BufferSize;
            //OptionKeepAlive = true;
            //OptionDualMode = true;
            OptionNoDelay = true;
            _serverAddress = address;
            _protocol = protocol;
            _buffer = new byte[LanProtocol.BufferSize];
        }

        //protected override Socket CreateSocket()
        //{
        //    return new Socket(Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        //    {
        //        EnableBroadcast = true
        //    };
        //}

        protected override void OnConnected()
        {
            //ReceiveAsync();
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LdnProxyTCPClient connected!");
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            _protocol.Read(ref _buffer, ref _bufferEnd, buffer, (int)offset, (int)size);
            //ReceiveAsync();
        }

        public void DisconnectAndStop()
        {
            //_stop = true;
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
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LdnProxyTCPClient caught an error with code {error}");
        }

        protected override void Dispose(bool disposingManagedResources)
        {
            DisconnectAndStop();
            base.Dispose(disposingManagedResources);
        }

        public bool Start()
        {
            return false;
        }

        public bool Stop()
        {
            return false;
        }
    }
}
