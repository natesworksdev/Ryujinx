using Ryujinx.Common;
using Ryujinx.Cpu;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Network;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Network.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using TcpClient = NetCoreServer.TcpClient;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator
{
    class Station : TcpClient
    {
        private const int FailureTimeout = 4000;
        private const int ScanTimeout = 1000;

        private bool _stop;

        public NetworkConfig CurrentNetworkConfig;
        public NetworkInfo   CurrentNetworkInfo;

        private ManualResetEvent _connected = new ManualResetEvent(false);
        private AutoResetEvent _apConnected = new AutoResetEvent(false);
        private AutoResetEvent _scan = new AutoResetEvent(false);
        private AutoResetEvent _error = new AutoResetEvent(false);

        private List<NetworkInfo> _availableGames;

        private IUserLocalCommunicationService _parent;

        public Station(IUserLocalCommunicationService parent, string address, int port) : base(address, port)
        {
            _parent = parent;

            ConnectAsync();

            _availableGames = new List<NetworkInfo>();
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

            // Wait for a while...
            Thread.Sleep(1000); // Required to rate limit scan, right now.

            if (!_stop)
            {
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
                case PacketId.ScanReply:    HandleScanReply(ldnHeader, LdnHelper.FromBytes<NetworkInfo>(incomingBuffer));   break;
                case PacketId.ScanReplyEnd: HandleScanReplyEnd(ldnHeader);                                                      break;
                case PacketId.Connected:    HandleConnected(ldnHeader, LdnHelper.FromBytes<NetworkInfo>(incomingBuffer));   break;
                case PacketId.SyncNetwork:  HandleSyncNetwork(ldnHeader, LdnHelper.FromBytes<NetworkInfo>(incomingBuffer)); break;

                default: break;
            }
        }

        private void HandleConnected(LdnHeader header, NetworkInfo info)
        {
            CurrentNetworkInfo = info;

            CurrentNetworkConfig = new NetworkConfig
            {
                IntentId = CurrentNetworkInfo.NetworkId.IntentId,
                Channel = CurrentNetworkInfo.Common.Channel,
                NodeCountMax = CurrentNetworkInfo.Ldn.NodeCountMax,
                Unknown1 = 0x00,
                LocalCommunicationVersion = (ushort)CurrentNetworkInfo.NetworkId.IntentId.LocalCommunicationId,
                Unknown2 = new byte[10]
            };

            _apConnected.Set();

            _parent.SetState(NetworkState.StationConnected);
        }

        private void HandleSyncNetwork(LdnHeader header, NetworkInfo info)
        {
            CurrentNetworkInfo = info;

            _parent.SetState();
        }

        private void HandleScanReply(LdnHeader header, NetworkInfo info)
        {
            _availableGames.Add(info);
        }

        private void HandleScanReplyEnd(LdnHeader obj)
        {
            _scan.Set();
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"LDN TCP client caught an error with code {error}");

            _error.Set();
        }

        public ResultCode Scan(ServiceCtx context)
        {
            uint       channel     = context.RequestData.ReadUInt32();
            uint       bufferCount = context.RequestData.ReadUInt32();
            ScanFilter scanFilter  = context.RequestData.ReadStruct<ScanFilter>();

            long bufferPosition = context.Request.ReceiveBuff[0].Position;
            long bufferSize     = context.Request.ReceiveBuff[0].Size;

            MemoryHelper.FillWithZeros(context.Memory, bufferPosition, (int)bufferSize);

            byte[] scanFilterBuffer       = LdnHelper.StructureToByteArray(scanFilter);
            int    scanFilterBufferLength = scanFilterBuffer.Length;

            Array.Resize(ref scanFilterBuffer, 0x600);

            int index = WaitHandle.WaitAny(new WaitHandle[] { _connected, _error }, FailureTimeout);

            if (index == 0)
            {
                LdnHeader ldnHeader = new LdnHeader
                {
                    Magic    = ('R' << 0) | ('L' << 8) | ('D' << 16) | ('N' << 24),
                    Type     = (byte)PacketId.Scan,
                    UserId   = LdnHelper.StringToByteArray("91ac8b112e1d4536a73c49f8eb9cb064"),
                    DataSize = scanFilterBufferLength
                };

                byte[] ldnPacket = LdnHelper.StructureToByteArray(ldnHeader);
                int ldnHeaderLength = ldnPacket.Length;

                Array.Resize(ref ldnPacket, ldnHeaderLength + scanFilterBuffer.Length);
                scanFilterBuffer.CopyTo(ldnPacket, ldnHeaderLength);

                SendAsync(ldnPacket);

                index = WaitHandle.WaitAny(new WaitHandle[] { _scan, _error }, ScanTimeout);
            }

            if (index != 0)
            {
                // An error occurred or timeout. Write 0 games.

                context.ResponseData.Write(0);

                return ResultCode.Success;
            }

            uint counter = 0;

            foreach (NetworkInfo networkInfo in _availableGames)
            {
                MemoryHelper.Write(context.Memory, bufferPosition + (Marshal.SizeOf(typeof(NetworkInfo)) * counter), networkInfo);

                counter++;
            }

            context.ResponseData.Write((long)counter);

            return ResultCode.Success;
        }

        public ResultCode Connect(ServiceCtx context)
        {
            ConnectNetworkData connectNetworkData = context.RequestData.ReadStruct<ConnectNetworkData>();

            long bufferPosition = context.Request.PtrBuff[0].Position;
            long bufferSize     = context.Request.PtrBuff[0].Size;

            byte[] networkInfoBytes = new byte[bufferSize];

            context.Memory.Read((ulong)bufferPosition, networkInfoBytes);

            NetworkInfo networkInfo = LdnHelper.FromBytes<NetworkInfo>(networkInfoBytes);

            ConnectRequest request = new ConnectRequest
            {
                Data = connectNetworkData,
                Info = networkInfo
            };

            byte[] requestBuffer = LdnHelper.StructureToByteArray(request);

            LdnHeader ldnHeader = new LdnHeader
            {
                Magic    = ('R' << 0) | ('L' << 8) | ('D' << 16) | ('N' << 24),
                Type     = (byte)PacketId.Connect,
                UserId   = LdnHelper.StringToByteArray("91ac8b112e1d4536a73c49f8eb9cb064"),
                DataSize = requestBuffer.Length
            };

            byte[] ldnPacket = LdnHelper.StructureToByteArray(ldnHeader);
            int ldnHeaderLength = ldnPacket.Length;

            Array.Resize(ref ldnPacket, ldnHeaderLength + requestBuffer.Length);
            requestBuffer.CopyTo(ldnPacket, ldnHeaderLength);

            SendAsync(ldnPacket);

            _apConnected.WaitOne(FailureTimeout);

            return ResultCode.Success;
        }
    }
}
