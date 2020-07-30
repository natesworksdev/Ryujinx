using Ryujinx.Common;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Network.Types;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using TcpClient = NetCoreServer.TcpClient;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator
{
    class AccessPoint : TcpClient
    {
        private const int FailureTimeout = 4000;

        private bool _stop;

        private byte[] _advertiseData;

        private IUserLocalCommunicationService _parent;

        private ManualResetEvent _connected = new ManualResetEvent(false);
        private AutoResetEvent _error = new AutoResetEvent(false);
        private AutoResetEvent _apConnected = new AutoResetEvent(false);

        public NetworkInfo NetworkInfo;

        public AccessPoint(IUserLocalCommunicationService parent, string address, int port) : base(address, port)
        {
            OptionNoDelay = true;

            _parent = parent;
        }

        public void DisconnectAndStop()
        {
            _stop = true;

            LdnHeader ldnHeader = new LdnHeader
            {
                Magic    = ('R' << 0) | ('L' << 8) | ('D' << 16) | ('N' << 24),
                Type     = (byte)PacketId.Disconnect,
                UserId   = LdnHelper.StringToByteArray("91ac8b112e1d4536a73c49f8eb9cb065"),
                DataSize = 0,
            };

            SendAsync(LdnHelper.StructureToByteArray(ldnHeader));

            DisconnectAsync();

            while (IsConnected)
            {
                Thread.Yield();
            }
        }

        protected override void OnConnected()
        {
            Console.WriteLine($"LDN TCP client connected a new session with Id {Id}");

            _connected.Set();
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"LDN TCP client disconnected a session with Id {Id}");

            _connected.Reset();

            if (!_stop)
            {
                // Wait for a while...
                Thread.Sleep(1000);

                // Try to connect again
                ConnectAsync();
            }
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            Console.WriteLine($"Incoming packet from {Id} (size: 0x{size.ToString("X2")}):");

            byte[] incomingBuffer = new byte[size];

            Buffer.BlockCopy(buffer, 0, incomingBuffer, 0, (int)size);

            LdnHeader ldnHeader = LdnHelper.FromBytes<LdnHeader>(incomingBuffer);

            incomingBuffer = incomingBuffer.Skip(Marshal.SizeOf(ldnHeader)).ToArray();

            switch ((PacketId)ldnHeader.Type)
            {
                case PacketId.SyncNetwork: HandleSyncNetwork(ldnHeader, LdnHelper.FromBytes<NetworkInfo>(incomingBuffer)); break;
                case PacketId.Connected:   HandleConnected(ldnHeader, LdnHelper.FromBytes<NetworkInfo>(incomingBuffer));   break;

                default: break;
            }
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"LDN TCP client caught an error with code {error}");

            _error.Set();
        }

        private void HandleSyncNetwork(LdnHeader header, NetworkInfo info)
        {
            NetworkInfo = info;

            _parent.SetState();
        }

        private void HandleConnected(LdnHeader header, NetworkInfo info)
        {
            NetworkInfo = info;

            _parent.SetState(NetworkState.AccessPointCreated);

            _apConnected.Set();
        }

        public ResultCode SetAdvertiseData(ServiceCtx context)
        {
            long bufferPosition = context.Request.PtrBuff[0].Position;
            long bufferSize     = context.Request.PtrBuff[0].Size;

            if (bufferSize > 0x180)
            {
                return ResultCode.InvalidArgument;
            }

            _advertiseData = new byte[bufferSize];

            context.Memory.Read((ulong)bufferPosition, _advertiseData);

            return ResultCode.Success;
        }

        public ResultCode CreateNetwork(ServiceCtx context)
        {
            SecurityConfig securityConfig = context.RequestData.ReadStruct<SecurityConfig>();
            UserConfig     userConfig     = context.RequestData.ReadStruct<UserConfig>();
            uint           reserved       = context.RequestData.ReadUInt32();
            NetworkConfig  networkConfig  = context.RequestData.ReadStruct<NetworkConfig>();

            ConnectAsync();

            CreateAccessPointRequest request = new CreateAccessPointRequest
            {
                SecurityConfig = securityConfig,
                UserConfig = userConfig,
                NetworkConfig = networkConfig
            };

            int index = WaitHandle.WaitAny(new WaitHandle[] { _connected, _error }, FailureTimeout);

            if (index != 0)
            {
                return ResultCode.Success;
            }

            byte[] requestBuffer = LdnHelper.StructureToByteArray(request);

            LdnHeader ldnHeader = new LdnHeader
            {
                Magic    = ('R' << 0) | ('L' << 8) | ('D' << 16) | ('N' << 24),
                Type     = (byte)PacketId.CreateAccessPoint,
                UserId   = LdnHelper.StringToByteArray("91ac8b112e1d4536a73c49f8eb9cb065"),
                DataSize = requestBuffer.Length + _advertiseData.Length,
            };

            byte[] ldnPacket = LdnHelper.StructureToByteArray(ldnHeader);
            int ldnHeaderLength = ldnPacket.Length;

            Array.Resize(ref ldnPacket, ldnHeaderLength + requestBuffer.Length + _advertiseData.Length);
            requestBuffer.CopyTo(ldnPacket, ldnHeaderLength);
            _advertiseData.CopyTo(ldnPacket, ldnHeaderLength + requestBuffer.Length);

            SendAsync(ldnPacket);

            _apConnected.WaitOne(FailureTimeout);

            return ResultCode.Success;
        }
    }
}
