using Ryujinx.Common.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd
{
    class ManagedSocket : ISocket
    {
        public AddressFamily AddressFamily => Socket.AddressFamily;

        public SocketType SocketType => Socket.SocketType;

        public ProtocolType ProtocolType => Socket.ProtocolType;

        public bool Blocking { get => Socket.Blocking; set => Socket.Blocking = value; }

        public IntPtr Handle => Socket.Handle;

        public IPEndPoint RemoteEndPoint => Socket.RemoteEndPoint as IPEndPoint;

        public IPEndPoint LocalEndPoint => Socket.LocalEndPoint as IPEndPoint;

        public Socket Socket { get; }

        private static readonly Dictionary<BsdSocketOption, SocketOptionName> _soSocketOptionMap = new()
        {
            { BsdSocketOption.SoDebug,       SocketOptionName.Debug },
            { BsdSocketOption.SoReuseAddr,   SocketOptionName.ReuseAddress },
            { BsdSocketOption.SoKeepAlive,   SocketOptionName.KeepAlive },
            { BsdSocketOption.SoDontRoute,   SocketOptionName.DontRoute },
            { BsdSocketOption.SoBroadcast,   SocketOptionName.Broadcast },
            { BsdSocketOption.SoUseLoopBack, SocketOptionName.UseLoopback },
            { BsdSocketOption.SoLinger,      SocketOptionName.Linger },
            { BsdSocketOption.SoOobInline,   SocketOptionName.OutOfBandInline },
            { BsdSocketOption.SoReusePort,   SocketOptionName.ReuseAddress },
            { BsdSocketOption.SoSndBuf,      SocketOptionName.SendBuffer },
            { BsdSocketOption.SoRcvBuf,      SocketOptionName.ReceiveBuffer },
            { BsdSocketOption.SoSndLoWat,    SocketOptionName.SendLowWater },
            { BsdSocketOption.SoRcvLoWat,    SocketOptionName.ReceiveLowWater },
            { BsdSocketOption.SoSndTimeo,    SocketOptionName.SendTimeout },
            { BsdSocketOption.SoRcvTimeo,    SocketOptionName.ReceiveTimeout },
            { BsdSocketOption.SoError,       SocketOptionName.Error },
            { BsdSocketOption.SoType,        SocketOptionName.Type }
        };

        private static readonly Dictionary<BsdSocketOption, SocketOptionName> _ipSocketOptionMap = new()
        {
            { BsdSocketOption.IpOptions,              SocketOptionName.IPOptions },
            { BsdSocketOption.IpHdrIncl,              SocketOptionName.HeaderIncluded },
            { BsdSocketOption.IpTtl,                  SocketOptionName.IpTimeToLive },
            { BsdSocketOption.IpMulticastIf,          SocketOptionName.MulticastInterface },
            { BsdSocketOption.IpMulticastTtl,         SocketOptionName.MulticastTimeToLive },
            { BsdSocketOption.IpMulticastLoop,        SocketOptionName.MulticastLoopback },
            { BsdSocketOption.IpAddMembership,        SocketOptionName.AddMembership },
            { BsdSocketOption.IpDropMembership,       SocketOptionName.DropMembership },
            { BsdSocketOption.IpDontFrag,             SocketOptionName.DontFragment },
            { BsdSocketOption.IpAddSourceMembership,  SocketOptionName.AddSourceMembership },
            { BsdSocketOption.IpDropSourceMembership, SocketOptionName.DropSourceMembership }
        };

        private static readonly Dictionary<BsdSocketOption, SocketOptionName> _tcpSocketOptionMap = new()
        {
            { BsdSocketOption.TcpNoDelay,   SocketOptionName.NoDelay },
            { BsdSocketOption.TcpKeepIdle,  SocketOptionName.TcpKeepAliveTime },
            { BsdSocketOption.TcpKeepIntvl, SocketOptionName.TcpKeepAliveInterval },
            { BsdSocketOption.TcpKeepCnt,   SocketOptionName.TcpKeepAliveRetryCount }
        };

        public ManagedSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            Socket = new Socket(addressFamily, socketType, protocolType);
        }

        private ManagedSocket(Socket socket)
        {
            Socket = socket;
        }

        private static SocketFlags ConvertBsdSocketFlags(BsdSocketFlags bsdSocketFlags)
        {
            SocketFlags socketFlags = SocketFlags.None;

            if (bsdSocketFlags.HasFlag(BsdSocketFlags.Oob))
            {
                socketFlags |= SocketFlags.OutOfBand;
            }

            if (bsdSocketFlags.HasFlag(BsdSocketFlags.Peek))
            {
                socketFlags |= SocketFlags.Peek;
            }

            if (bsdSocketFlags.HasFlag(BsdSocketFlags.DontRoute))
            {
                socketFlags |= SocketFlags.DontRoute;
            }

            if (bsdSocketFlags.HasFlag(BsdSocketFlags.Trunc))
            {
                socketFlags |= SocketFlags.Truncated;
            }

            if (bsdSocketFlags.HasFlag(BsdSocketFlags.CTrunc))
            {
                socketFlags |= SocketFlags.ControlDataTruncated;
            }

            bsdSocketFlags &= ~(BsdSocketFlags.Oob |
                BsdSocketFlags.Peek |
                BsdSocketFlags.DontRoute |
                BsdSocketFlags.DontWait |
                BsdSocketFlags.Trunc |
                BsdSocketFlags.CTrunc);

            if (bsdSocketFlags != BsdSocketFlags.None)
            {
                Logger.Warning?.Print(LogClass.ServiceBsd, $"Unsupported socket flags: {bsdSocketFlags}");
            }

            return socketFlags;
        }

        private static bool TryConvertSocketOption(BsdSocketOption option, SocketOptionLevel level, out SocketOptionName name)
        {
            var table = level switch
            {
                SocketOptionLevel.Socket => _soSocketOptionMap,
                SocketOptionLevel.IP => _ipSocketOptionMap,
                SocketOptionLevel.Tcp => _tcpSocketOptionMap,
                _ => null
            };

            if (table == null)
            {
                name = default;
                return false;
            }

            return table.TryGetValue(option, out name);
        }

        public LinuxError Accept(out ISocket newSocket)
        {
            try
            {
                newSocket = new ManagedSocket(Socket.Accept());

                return LinuxError.SUCCESS;
            }
            catch (SocketException exception)
            {
                newSocket = null;

                return WinSockHelper.ConvertError((WsaError)exception.ErrorCode);
            }
        }

        public LinuxError Bind(IPEndPoint localEndPoint)
        {
            try
            {
                Socket.Bind(localEndPoint);

                return LinuxError.SUCCESS;
            }
            catch (SocketException exception)
            {
                return WinSockHelper.ConvertError((WsaError)exception.ErrorCode);
            }
        }

        public void Close()
        {
            Socket.Close();
        }

        public LinuxError Connect(IPEndPoint remoteEndPoint)
        {
            try
            {
                Socket.Connect(remoteEndPoint);

                return LinuxError.SUCCESS;
            }
            catch (SocketException exception)
            {
                if (!Blocking && exception.ErrorCode == (int)WsaError.WSAEWOULDBLOCK)
                {
                    return LinuxError.EINPROGRESS;
                }
                else
                {
                    return WinSockHelper.ConvertError((WsaError)exception.ErrorCode);
                }
            }
        }

        public void Disconnect()
        {
            Socket.Disconnect(true);
        }

        public void Dispose()
        {
            Socket.Dispose();
        }

        public LinuxError Listen(int backlog)
        {
            try
            {
                Socket.Listen(backlog);

                return LinuxError.SUCCESS;
            }
            catch (SocketException exception)
            {
                return WinSockHelper.ConvertError((WsaError)exception.ErrorCode);
            }
        }

        public bool Poll(int microSeconds, SelectMode mode)
        {
            return Socket.Poll(microSeconds, mode);
        }

        public LinuxError Shutdown(BsdSocketShutdownFlags how)
        {
            try
            {
                Socket.Shutdown((SocketShutdown)how);

                return LinuxError.SUCCESS;
            }
            catch (SocketException exception)
            {
                return WinSockHelper.ConvertError((WsaError)exception.ErrorCode);
            }
        }

        public LinuxError Receive(out int receiveSize, Span<byte> buffer, BsdSocketFlags flags)
        {
            try
            {
                receiveSize = Socket.Receive(buffer, ConvertBsdSocketFlags(flags));

                return LinuxError.SUCCESS;
            }
            catch (SocketException exception)
            {
                receiveSize = -1;

                return WinSockHelper.ConvertError((WsaError)exception.ErrorCode);
            }
        }

        public LinuxError ReceiveFrom(out int receiveSize, Span<byte> buffer, int size, BsdSocketFlags flags, out IPEndPoint remoteEndPoint)
        {
            remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            LinuxError result;

            bool shouldBlockAfterOperation = false;

            try
            {
                EndPoint temp = new IPEndPoint(IPAddress.Any, 0);

                if (Blocking && flags.HasFlag(BsdSocketFlags.DontWait))
                {
                    Blocking = false;
                    shouldBlockAfterOperation = true;
                }

                receiveSize = Socket.ReceiveFrom(buffer[..size], ConvertBsdSocketFlags(flags), ref temp);

                remoteEndPoint = (IPEndPoint)temp;
                result = LinuxError.SUCCESS;
            }
            catch (SocketException exception)
            {
                receiveSize = -1;

                result = WinSockHelper.ConvertError((WsaError)exception.ErrorCode);
            }

            if (shouldBlockAfterOperation)
            {
                Blocking = true;
            }

            return result;
        }

        public LinuxError Send(out int sendSize, ReadOnlySpan<byte> buffer, BsdSocketFlags flags)
        {
            try
            {
                sendSize = Socket.Send(buffer, ConvertBsdSocketFlags(flags));

                return LinuxError.SUCCESS;
            }
            catch (SocketException exception)
            {
                sendSize = -1;

                return WinSockHelper.ConvertError((WsaError)exception.ErrorCode);
            }
        }

        public LinuxError SendTo(out int sendSize, ReadOnlySpan<byte> buffer, int size, BsdSocketFlags flags, IPEndPoint remoteEndPoint)
        {
            try
            {
                sendSize = Socket.SendTo(buffer[..size], ConvertBsdSocketFlags(flags), remoteEndPoint);

                return LinuxError.SUCCESS;
            }
            catch (SocketException exception)
            {
                sendSize = -1;

                return WinSockHelper.ConvertError((WsaError)exception.ErrorCode);
            }
        }

        public LinuxError GetSocketOption(BsdSocketOption option, SocketOptionLevel level, Span<byte> optionValue)
        {
            try
            {
                if (!TryConvertSocketOption(option, level, out SocketOptionName optionName))
                {
                    Logger.Warning?.Print(LogClass.ServiceBsd, $"Unsupported GetSockOpt Option: {option} Level: {level}");

                    return LinuxError.EOPNOTSUPP;
                }

                byte[] tempOptionValue = new byte[optionValue.Length];

                Socket.GetSocketOption(level, optionName, tempOptionValue);

                tempOptionValue.AsSpan().CopyTo(optionValue);

                return LinuxError.SUCCESS;
            }
            catch (SocketException exception)
            {
                return WinSockHelper.ConvertError((WsaError)exception.ErrorCode);
            }
        }

        public LinuxError SetSocketOption(BsdSocketOption option, SocketOptionLevel level, ReadOnlySpan<byte> optionValue)
        {
            try
            {
                if (!TryConvertSocketOption(option, level, out SocketOptionName optionName))
                {
                    Logger.Warning?.Print(LogClass.ServiceBsd, $"Unsupported SetSockOpt Option: {option} Level: {level}");

                    return LinuxError.EOPNOTSUPP;
                }

                int value = MemoryMarshal.Read<int>(optionValue);

                if (option == BsdSocketOption.SoLinger)
                {
                    int value2 = MemoryMarshal.Read<int>(optionValue[4..]);

                    Socket.SetSocketOption(level, SocketOptionName.Linger, new LingerOption(value != 0, value2));
                }
                else
                {
                    Socket.SetSocketOption(level, optionName, value);
                }

                return LinuxError.SUCCESS;
            }
            catch (SocketException exception)
            {
                return WinSockHelper.ConvertError((WsaError)exception.ErrorCode);
            }
        }
    }
}
