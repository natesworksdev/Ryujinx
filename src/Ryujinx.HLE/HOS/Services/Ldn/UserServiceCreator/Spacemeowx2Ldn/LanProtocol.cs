using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Ldn.Spacemeowx2Ldn;
using Ryujinx.HLE.HOS.Services.Ldn.Spacemeowx2Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Spacemeowx2Ldn.Proxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Spacemeowx2Ldn
{
    internal class LanProtocol
    {
        public const ulong SsidLengthMax = 32;
        public const ulong AdvertiseDataSizeMax = 384;
        public const ulong UserNameBytesMax = 32;
        public const int NodeCountMax = 8;
        public const int StationCountMax = NodeCountMax - 1;
        public const ulong PassphraseLengthMax = 64;

        public const int BufferSize = 2048;
        protected const uint LanMagic = 0x11451400;

        private readonly int _headerSize = Marshal.SizeOf<LanPacketHeader>();

        private readonly LanDiscovery _discovery;

        private readonly IPAddress _localBroadcastAddr;

        public event Action<LdnProxyTcpSession> Accept;
        public event Action<EndPoint, LanPacketType, byte[]> Scan;
        public event Action<NetworkInfo> ScanResponse;
        public event Action<NetworkInfo> SyncNetwork;
        public event Action<NodeInfo, EndPoint> Connect;
        public event Action<LdnProxyTcpSession> DisconnectStation;

        // Source: https://stackoverflow.com/a/39338188
        private static IPAddress GetBroadcastAddress(IPAddress address, IPAddress mask)
        {
            uint ipAddress = BitConverter.ToUInt32(address.GetAddressBytes(), 0);
            uint ipMaskV4 = BitConverter.ToUInt32(mask.GetAddressBytes(), 0);
            uint broadCastIpAddress = ipAddress | ~ipMaskV4;

            return new IPAddress(BitConverter.GetBytes(broadCastIpAddress));
        }

        public LanProtocol(LanDiscovery parent)
        {
            _discovery = parent;
            _localBroadcastAddr = GetBroadcastAddress(_discovery.localAddr, _discovery.localAddrMask);
        }

        public void InvokeAccept(LdnProxyTcpSession session)
        {
            Accept?.Invoke(session);
        }

        public void InvokeDisconnectStation(LdnProxyTcpSession session)
        {
            DisconnectStation?.Invoke(session);
        }

        private void DecodeAndHandle(LanPacketHeader header, byte[] data, EndPoint endPoint = null)
        {
            switch (header.Type)
            {
                case LanPacketType.Scan:
                    // UDP
                    Logger.Info?.PrintMsg(LogClass.ServiceLdn, "Sending ScanResponse...");
                    Scan?.Invoke(endPoint, LanPacketType.ScanResponse, LdnHelper.StructureToByteArray(_discovery.networkInfo));
                    break;
                case LanPacketType.ScanResponse:
                    // UDP
                    Logger.Info?.PrintMsg(LogClass.ServiceLdn, "Got ScanResponse.");
                    ScanResponse?.Invoke(LdnHelper.FromBytes<NetworkInfo>(data));
                    break;
                case LanPacketType.SyncNetwork:
                    // TCP
                    Logger.Info?.PrintMsg(LogClass.ServiceLdn, "Got SyncNetwork.");
                    SyncNetwork?.Invoke(LdnHelper.FromBytes<NetworkInfo>(data));
                    break;
                case LanPacketType.Connect:
                    // TCP Session / Station
                    Logger.Info?.PrintMsg(LogClass.ServiceLdn, "Got Connect.");
                    Connect?.Invoke(LdnHelper.FromBytes<NodeInfo>(data), endPoint);
                    break;
                default:
                    Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"Decode error: unhandled type {header.Type}");
                    break;
            }
        }

        public void Read(ref byte[] _buffer, ref int _bufferEnd, byte[] data, int offset, int size, EndPoint endPoint = null)
        {
            if (endPoint != null && _discovery.localAddr.Equals(((IPEndPoint)endPoint).Address))
            {
                return;
            }

            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"LanProtocol Reading data: EP: {endPoint} Offset: {offset} Size: {size}");

            // Scan packet
            if (size == 12)
            {
                DecodeAndHandle(PrepareHeader(new LanPacketHeader(), LanPacketType.Scan), Array.Empty<byte>(), endPoint);
                _bufferEnd = 0;
                return;
            }

            int index = 0;
            while (index < size)
            {
                if (_bufferEnd < _headerSize)
                {
                    int copyable2 = Math.Min(size - index, Math.Min(size, _headerSize - _bufferEnd));
                    Array.Copy(data, index + offset, _buffer, _bufferEnd, copyable2);
                    index += copyable2;
                    _bufferEnd += copyable2;
                }
                if (_bufferEnd >= _headerSize)
                {
                    LanPacketHeader header = LdnHelper.FromBytes<LanPacketHeader>(_buffer);
                    Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"Received packet info: [length: {_bufferEnd}] [Header size: {header.Length}]");
                    if (header.Magic != LanMagic)
                    {
                        _bufferEnd = 0;
                        Logger.Warning?.PrintMsg(LogClass.ServiceLdn, $"Invalid magic number in received packet. [magic: {header.Magic}] [EP: {endPoint}]");
                        return;
                    }
                    int totalSize = _headerSize + header.Length;
                    if (totalSize > BufferSize)
                    {
                        _bufferEnd = 0;
                        Logger.Warning?.PrintMsg(LogClass.ServiceLdn, $"Max packet size {BufferSize} exceeded.");
                        return;
                    }

                    int copyable = Math.Min(size - index, Math.Min(size, totalSize - _bufferEnd));
                    Array.Copy(data, index + offset, _buffer, _bufferEnd, copyable);
                    index += copyable;
                    _bufferEnd += copyable;
                    if (totalSize == _bufferEnd)
                    {
                        Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"Packet received from: {endPoint}");

                        byte[] ldnData = new byte[totalSize - _headerSize];
                        Array.Copy(_buffer, _headerSize, ldnData, 0, ldnData.Length);

                        if (header.Compressed == 1)
                        {
                            if (Decompress(ldnData, out byte[] decompressedLdnData) != 0)
                            {
                                Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"Decompress error:\n {header}, {_headerSize}\n {ldnData}, {ldnData.Length}");
                                return;
                            }
                            if (decompressedLdnData.Length != header.DecompressLength)
                            {
                                Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"Decompress error: length does not match. ({decompressedLdnData.Length} != {header.DecompressLength})");
                                Logger.Error?.PrintMsg(LogClass.ServiceLdn, $"Decompress error data: '{string.Join("", decompressedLdnData.Select(x => (int)x).ToArray())}'");
                                return;
                            }
                            ldnData = decompressedLdnData;
                        }

                        DecodeAndHandle(header, ldnData, endPoint);
                        _bufferEnd = 0;
                    }
                }
            }
        }

        public int SendBroadcast(ILdnSocket s, LanPacketType type, int port)
        {
            return SendPacket(s, type, Array.Empty<byte>(), new IPEndPoint(_localBroadcastAddr, port));
        }

        public int SendPacket(ILdnSocket s, LanPacketType type, byte[] data, EndPoint endPoint = null)
        {
            byte[] buf = PreparePacket(type, data);
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"Sending '{type}' packet... [size: {buf.Length}]");
            return s.SendPacketAsync(endPoint, buf) ? 0 : -1;
        }

        public int SendPacket(LdnProxyTcpSession s, LanPacketType type, byte[] data)
        {
            byte[] buf = PreparePacket(type, data);
            Logger.Info?.PrintMsg(LogClass.ServiceLdn, $"Sending packet [size: {buf.Length}] to LdnProxyTcpSession: {s.Socket.RemoteEndPoint}");
            return s.SendAsync(buf) ? 0 : -1;
        }

        private LanPacketHeader PrepareHeader(LanPacketHeader header, LanPacketType type)
        {
            header.Magic = LanMagic;
            header.Type = type;
            header.Compressed = 0;
            header.Length = 0;
            header.DecompressLength = 0;
            header.Reserved = new byte[2];

            return header;
        }

        private byte[] PreparePacket(LanPacketType type, byte[] data)
        {
            LanPacketHeader header = PrepareHeader(new LanPacketHeader(), type);
            header.Length = (ushort)data.Length;
            byte[] buf = new byte[data.Length + _headerSize];
            if (data.Length > 0)
            {
                if (Compress(data, out byte[] compressed) == 0)
                {
                    header.DecompressLength = header.Length;
                    header.Length = (ushort)compressed.Length;
                    header.Compressed = 1;

                    LdnHelper.StructureToByteArray(header).CopyTo(buf, 0);
                    compressed.CopyTo(buf, _headerSize);
                }
                else
                {
                    LdnHelper.StructureToByteArray(header).CopyTo(buf, 0);
                    data.CopyTo(buf, _headerSize);
                }
            }
            else
            {
                LdnHelper.StructureToByteArray(header).CopyTo(buf, 0);
            }

            return buf;
        }

        private int Compress(byte[] input, out byte[] output)
        {
            List<byte> outputList = new List<byte>();
            int i = 0;
            int maxCount = 0xFF;

            while (i < input.Length)
            {
                byte c = input[i];
                i++;
                int count = 0;

                if (c == 0)
                {
                    while (i < input.Length && input[i] == 0 && count < maxCount)
                    {
                        count += 1;
                        i++;
                    }
                }

                if (c == 0)
                {
                    outputList.Add(0);

                    if (outputList.Count == BufferSize)
                    {
                        output = null;
                        return -1;
                    }
                    outputList.Add((byte)count);
                }
                else
                {
                    outputList.Add(c);
                }
            }

            output = outputList.ToArray();
            return i == input.Length ? 0 : -1;
        }

        private int Decompress(byte[] input, out byte[] output)
        {
            List<byte> outputList = new List<byte>();
            int i = 0;
            while (i < input.Length && outputList.Count < BufferSize)
            {
                byte c = input[i];
                i++;

                outputList.Add(c);
                if (c == 0)
                {
                    if (i == input.Length)
                    {
                        output = null;
                        return -1;
                    }

                    int count = input[i];
                    i++;
                    for (int j = 0; j < count; j++)
                    {
                        if (outputList.Count == BufferSize)
                        {
                            break;
                        }
                        outputList.Add(c);
                    }
                }
            }

            output = outputList.ToArray();
            return i == input.Length ? 0 : -1;
        }
    }
}
