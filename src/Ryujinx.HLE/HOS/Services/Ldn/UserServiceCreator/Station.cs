using Ryujinx.Common;
using Ryujinx.Cpu;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Types;
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
        private bool _stop;

        public NetworkConfig CurrentNetworkConfig;
        public NetworkInfo   CurrentNetworkInfo;

        private AutoResetEvent ConnectEvent = new AutoResetEvent(false);
        private AutoResetEvent ScanEvent = new AutoResetEvent(false);

        private List<NetworkInfo> _availableGames;

        public Station(string address, int port) : base(address, port)
        {
            ConnectAsync();

            _availableGames = new List<NetworkInfo>();
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
                case PacketId.ScanReply:    ProcessScanReply(ldnPacket); break;
                case PacketId.ScanReplyEnd: ProcessScanReplyEnd();       break;
                case PacketId.Connected:    ProcessConnected(ldnPacket); break;

                default: break;
            }
        }

        private void ProcessScanReply(LdnPacket ldnPacket)
        {
            byte[] networkInfoBuffer = new byte[Marshal.SizeOf(typeof(NetworkInfo))];

            Buffer.BlockCopy(ldnPacket.Data, 0, networkInfoBuffer, 0, networkInfoBuffer.Length);

            _availableGames.Add(LdnHelper.FromBytes<NetworkInfo>(networkInfoBuffer));
        }

        private void ProcessScanReplyEnd()
        {
            ScanEvent.Set();
        }

        private void ProcessConnected(LdnPacket ldnPacket)
        {
            byte[] networkInfoBuffer = new byte[Marshal.SizeOf(typeof(NetworkInfo))];

            Buffer.BlockCopy(ldnPacket.Data, 0, networkInfoBuffer, 0, networkInfoBuffer.Length);

            CurrentNetworkInfo = LdnHelper.FromBytes<NetworkInfo>(networkInfoBuffer);

            CurrentNetworkConfig = new NetworkConfig
            {
                IntentId                  = CurrentNetworkInfo.NetworkId.IntentId,
                Channel                   = CurrentNetworkInfo.Common.Channel,
                NodeCountMax              = CurrentNetworkInfo.Ldn.NodeCountMax,
                Unknown1                  = 0x00,
                LocalCommunicationVersion = (ushort)CurrentNetworkInfo.NetworkId.IntentId.LocalCommunicationId,
                Unknown2                  = new byte[10]
            };

            ConnectEvent.Set();
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"LDN TCP client caught an error with code {error}");
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

            LdnPacket ldnPacket = new LdnPacket
            {
                Magic    = ('R' << 0) | ('L' << 8) | ('D' << 16) | ('N' << 24),
                Type     = (byte)PacketId.Scan,
                UserId   = LdnHelper.StringToByteArray("91ac8b112e1d4536a73c49f8eb9cb064"),
                DataSize = scanFilterBufferLength,
                Data     = scanFilterBuffer
            };

            SendAsync(LdnHelper.StructureToByteArray(ldnPacket));

            ScanEvent.WaitOne(1000);

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

            byte[] connectNetworkDataBuffer = LdnHelper.StructureToByteArray(connectNetworkData);

            long bufferPosition = context.Request.PtrBuff[0].Position;
            long bufferSize     = context.Request.PtrBuff[0].Size;

            byte[] networkInfo = new byte[bufferSize];

            context.Memory.Read((ulong)bufferPosition, networkInfo);

            byte[] ldnPacketBuffer = connectNetworkDataBuffer.Concat(networkInfo).ToArray();

            Array.Resize(ref ldnPacketBuffer, 0x600);

            LdnPacket ldnPacket = new LdnPacket
            {
                Magic    = ('R' << 0) | ('L' << 8) | ('D' << 16) | ('N' << 24),
                Type     = (byte)PacketId.Connect,
                UserId   = LdnHelper.StringToByteArray("91ac8b112e1d4536a73c49f8eb9cb064"),
                DataSize = connectNetworkDataBuffer.Length + networkInfo.Length,
                Data     = ldnPacketBuffer
            };

            SendAsync(LdnHelper.StructureToByteArray(ldnPacket));

            ConnectEvent.WaitOne(1000);

            return ResultCode.Success;
        }
    }
}