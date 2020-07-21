using Ryujinx.Common;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Types;
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
        private bool _stop;

        private byte[] _advertiseData;

        private KEvent _stateChangeEvent;

        public NetworkInfo NetworkInfo;


        public AccessPoint(string address, int port, KEvent stateChangeEvent) : base(address, port)
        {
            _stateChangeEvent = stateChangeEvent;
        }

        public void DisconnectAndStop()
        {
            _stop = true;

            DisconnectAsync();

            while (IsConnected)
            {
                Thread.Yield();
            }
        }

        protected override void OnConnected()
        {
            Console.WriteLine($"LDN TCP client connected a new session with Id {Id}");
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"LDN TCP client disconnected a session with Id {Id}");

            // Wait for a while...
            Thread.Sleep(1000);

            // Try to connect again
            if (!_stop)
            {
                ConnectAsync();
            }
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            Console.WriteLine($"Incoming packet from {Id} (size: 0x{size.ToString("X2")}):");

            byte[] incomingBuffer = new byte[size];

            Buffer.BlockCopy(buffer, 0, incomingBuffer, 0, (int)size);

            LdnPacket ldnPacket = LdnHelper.FromBytes<LdnPacket>(incomingBuffer);

            switch ((PacketId)ldnPacket.Type)
            {
                case PacketId.SyncNetwork: ParseSyncNetwork(ldnPacket); break;
                case PacketId.Connected:   ParseConnected(ldnPacket);   break;

                default: break;
            }
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"LDN TCP client caught an error with code {error}");
        }

        private void ParseSyncNetwork(LdnPacket ldnPacket)
        {
            byte[] networkInfoBuffer = new byte[Marshal.SizeOf(typeof(NetworkInfo))];

            Buffer.BlockCopy(ldnPacket.Data, 0, networkInfoBuffer, 0, networkInfoBuffer.Length);

            NetworkInfo = LdnHelper.FromBytes<NetworkInfo>(networkInfoBuffer);
        }

        private void ParseConnected(LdnPacket ldnPacket)
        {
            byte[] networkInfoBuffer = new byte[Marshal.SizeOf(typeof(NetworkInfo))];

            Buffer.BlockCopy(ldnPacket.Data, 0, networkInfoBuffer, 0, networkInfoBuffer.Length);

            NetworkInfo = LdnHelper.FromBytes<NetworkInfo>(networkInfoBuffer);

            _stateChangeEvent.WritableEvent.Signal();
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

            byte[] securityConfigBuffer = LdnHelper.StructureToByteArray(securityConfig);
            byte[] userConfigBuffer     = LdnHelper.StructureToByteArray(userConfig);
            byte[] networkConfigBuffer  = LdnHelper.StructureToByteArray(networkConfig);

            byte[] ldnPacketBuffer = securityConfigBuffer.Concat(userConfigBuffer).Concat(networkConfigBuffer).Concat(_advertiseData).ToArray();

            Array.Resize(ref ldnPacketBuffer, 0x600);

            LdnPacket ldnPacket = new LdnPacket
            {
                Magic    = ('R' << 0) | ('L' << 8) | ('D' << 16) | ('N' << 24),
                Type     = (byte)PacketId.CreateAccessPoint,
                UserId   = LdnHelper.StringToByteArray("91ac8b112e1d4536a73c49f8eb9cb065"),
                DataSize = securityConfigBuffer.Length + userConfigBuffer.Length + networkConfigBuffer.Length + _advertiseData.Length,
                Data     = ldnPacketBuffer
            };

            SendAsync(LdnHelper.StructureToByteArray(ldnPacket));

            return ResultCode.Success;
        }
    }
}