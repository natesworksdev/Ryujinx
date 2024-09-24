using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Sockets.Bsd.Types;
using RyuSocks;
using RyuSocks.Auth;
using RyuSocks.Commands;
using RyuSocks.Types;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd.Impl
{
    class ManagedProxySocket : ISocket
    {
        private static readonly Dictionary<AuthMethod, IProxyAuth> _authMethods = new()
        {
            { AuthMethod.NoAuth, new NoAuth() },
        };

        private readonly bool _isUdpSocket;
        private readonly bool _acceptedConnection;

        public SocksClient ProxyClient { get; }

        public bool Blocking { get => ProxyClient.Blocking; set => ProxyClient.Blocking = value; }
        public int RefCount { get; set; }

        public IPEndPoint RemoteEndPoint => (IPEndPoint)ProxyClient.ProxiedRemoteEndPoint;
        public IPEndPoint LocalEndPoint => (IPEndPoint)ProxyClient.ProxiedLocalEndPoint;

        public AddressFamily AddressFamily => ProxyClient.AddressFamily;
        public SocketType SocketType => ProxyClient.SocketType;
        public ProtocolType ProtocolType => ProxyClient.ProtocolType;
        public IntPtr Handle => throw new NotSupportedException("Can't get the handle of a proxy socket.");

        public ManagedProxySocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, EndPoint proxyEndpoint)
        {
            if (addressFamily != proxyEndpoint.AddressFamily && addressFamily != AddressFamily.Unspecified)
            {
                throw new ArgumentException(
                    $"Invalid {nameof(System.Net.Sockets.AddressFamily)}", nameof(addressFamily));
            }

            if (socketType != SocketType.Stream && socketType != SocketType.Dgram)
            {
                throw new ArgumentException(
                    $"Invalid {nameof(System.Net.Sockets.SocketType)}", nameof(socketType));
            }

            if (protocolType != ProtocolType.Tcp && protocolType != ProtocolType.Udp)
            {
                throw new ArgumentException(
                    $"Invalid {nameof(System.Net.Sockets.ProtocolType)}", nameof(protocolType));
            }

            _isUdpSocket = socketType == SocketType.Dgram && protocolType == ProtocolType.Udp;

            ProxyClient = proxyEndpoint switch
            {
                IPEndPoint ipEndPoint => new SocksClient(ipEndPoint) { OfferedAuthMethods = _authMethods },
                DnsEndPoint dnsEndPoint => new SocksClient(dnsEndPoint) { OfferedAuthMethods = _authMethods },
                _ => throw new ArgumentException($"Unsupported {nameof(EndPoint)} type", nameof(proxyEndpoint))
            };

            ProxyClient.Authenticate();

            RefCount = 1;
        }

        private ManagedProxySocket(SocksClient proxyClient)
        {
            ProxyClient = proxyClient;
            _acceptedConnection = true;
            RefCount = 1;
        }

        private static LinuxError ToLinuxError(ReplyField proxyReply)
        {
            return proxyReply switch
            {
                ReplyField.Succeeded => LinuxError.SUCCESS,
                ReplyField.ServerFailure => LinuxError.ECONNRESET,
                ReplyField.ConnectionNotAllowed => LinuxError.ECONNREFUSED,
                ReplyField.NetworkUnreachable => LinuxError.ENETUNREACH,
                ReplyField.HostUnreachable => LinuxError.EHOSTUNREACH,
                ReplyField.ConnectionRefused => LinuxError.ECONNREFUSED,
                ReplyField.TTLExpired => LinuxError.EHOSTUNREACH,
                ReplyField.CommandNotSupported => LinuxError.EOPNOTSUPP,
                ReplyField.AddressTypeNotSupported => LinuxError.EAFNOSUPPORT,
                _ => throw new ArgumentOutOfRangeException(nameof(proxyReply))
            };
        }

        public void Dispose()
        {
            ProxyClient.Dispose();
        }

        public LinuxError Read(out int readSize, Span<byte> buffer)
        {
            return Receive(out readSize, buffer, BsdSocketFlags.None);
        }

        public LinuxError Write(out int writeSize, ReadOnlySpan<byte> buffer)
        {
            return Send(out writeSize, buffer, BsdSocketFlags.None);
        }

        public LinuxError Receive(out int receiveSize, Span<byte> buffer, BsdSocketFlags flags)
        {
            LinuxError result;
            bool shouldBlockAfterOperation = false;

            if (Blocking && flags.HasFlag(BsdSocketFlags.DontWait))
            {
                Blocking = false;
                shouldBlockAfterOperation = true;
            }

            byte[] proxyBuffer = new byte[buffer.Length + ProxyClient.GetRequiredWrapperSpace()];

            try
            {
                receiveSize = ProxyClient.Receive(
                    proxyBuffer,
                    WinSockHelper.ConvertBsdSocketFlags(flags),
                    out SocketError errorCode
                );

                proxyBuffer[..receiveSize].CopyTo(buffer);

                result = WinSockHelper.ConvertError((WsaError)errorCode);
            }
            catch (ProxyException exception)
            {
                Logger.Error?.Print(
                    LogClass.ServiceBsd,
                    $"An error occured while trying to receive data: {exception}"
                );

                receiveSize = -1;
                result = ToLinuxError(exception.ReplyCode);
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

        public LinuxError ReceiveFrom(out int receiveSize, Span<byte> buffer, int size, BsdSocketFlags flags, out IPEndPoint remoteEndPoint)
        {
            LinuxError result;
            remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            bool shouldBlockAfterOperation = false;

            byte[] proxyBuffer = new byte[size + ProxyClient.GetRequiredWrapperSpace()];

            if (Blocking && flags.HasFlag(BsdSocketFlags.DontWait))
            {
                Blocking = false;
                shouldBlockAfterOperation = true;
            }

            try
            {
                EndPoint temp = new IPEndPoint(IPAddress.Any, 0);

                receiveSize = ProxyClient.ReceiveFrom(proxyBuffer, WinSockHelper.ConvertBsdSocketFlags(flags), ref temp);

                proxyBuffer[..receiveSize].CopyTo(buffer);

                remoteEndPoint = (IPEndPoint)temp;
                result = LinuxError.SUCCESS;
            }
            catch (ProxyException exception)
            {
                Logger.Error?.Print(
                    LogClass.ServiceBsd,
                    $"An error occured while trying to receive data: {exception}"
                );

                receiveSize = -1;
                result = ToLinuxError(exception.ReplyCode);
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
                sendSize = ProxyClient.Send(buffer, WinSockHelper.ConvertBsdSocketFlags(flags));

                return LinuxError.SUCCESS;
            }
            catch (ProxyException exception)
            {
                Logger.Error?.Print(
                    LogClass.ServiceBsd,
                    $"An error occured while trying to send data: {exception}"
                );

                sendSize = -1;

                return ToLinuxError(exception.ReplyCode);
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
                // NOTE: sendSize might be larger than size and/or buffer.Length.
                sendSize = ProxyClient.SendTo(buffer[..size], WinSockHelper.ConvertBsdSocketFlags(flags), remoteEndPoint);

                return LinuxError.SUCCESS;
            }
            catch (ProxyException exception)
            {
                Logger.Error?.Print(
                    LogClass.ServiceBsd,
                    $"An error occured while trying to send data: {exception}"
                );

                sendSize = -1;

                return ToLinuxError(exception.ReplyCode);
            }
            catch (SocketException exception)
            {
                sendSize = -1;

                return WinSockHelper.ConvertError((WsaError)exception.ErrorCode);
            }
        }

        public LinuxError RecvMMsg(out int vlen, BsdMMsgHdr message, BsdSocketFlags flags, TimeVal timeout)
        {
            throw new NotImplementedException();
        }

        public LinuxError SendMMsg(out int vlen, BsdMMsgHdr message, BsdSocketFlags flags)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adapted from <see cref="ManagedSocket.GetSocketOption"/>
        /// </summary>
        public LinuxError GetSocketOption(BsdSocketOption option, SocketOptionLevel level, Span<byte> optionValue)
        {
            try
            {
                LinuxError result = WinSockHelper.ValidateSocketOption(option, level, write: false);

                if (result != LinuxError.SUCCESS)
                {
                    Logger.Warning?.Print(LogClass.ServiceBsd, $"Invalid GetSockOpt Option: {option} Level: {level}");

                    return result;
                }

                if (!WinSockHelper.TryConvertSocketOption(option, level, out SocketOptionName optionName))
                {
                    Logger.Warning?.Print(LogClass.ServiceBsd, $"Unsupported GetSockOpt Option: {option} Level: {level}");
                    optionValue.Clear();

                    return LinuxError.SUCCESS;
                }

                byte[] tempOptionValue = new byte[optionValue.Length];

                ProxyClient.GetSocketOption(level, optionName, tempOptionValue);

                tempOptionValue.AsSpan().CopyTo(optionValue);

                return LinuxError.SUCCESS;
            }
            catch (SocketException exception)
            {
                return WinSockHelper.ConvertError((WsaError)exception.ErrorCode);
            }
        }

        /// <summary>
        /// Adapted from <see cref="ManagedSocket.SetSocketOption"/>
        /// </summary>
        public LinuxError SetSocketOption(BsdSocketOption option, SocketOptionLevel level, ReadOnlySpan<byte> optionValue)
        {
            try
            {
                LinuxError result = WinSockHelper.ValidateSocketOption(option, level, write: true);

                if (result != LinuxError.SUCCESS)
                {
                    Logger.Warning?.Print(LogClass.ServiceBsd, $"Invalid SetSockOpt Option: {option} Level: {level}");

                    return result;
                }

                if (!WinSockHelper.TryConvertSocketOption(option, level, out SocketOptionName optionName))
                {
                    Logger.Warning?.Print(LogClass.ServiceBsd, $"Unsupported SetSockOpt Option: {option} Level: {level}");

                    return LinuxError.SUCCESS;
                }

                byte[] value = optionValue.ToArray();

                ProxyClient.SetSocketOption(level, optionName, value);

                return LinuxError.SUCCESS;
            }
            catch (SocketException exception)
            {
                return WinSockHelper.ConvertError((WsaError)exception.ErrorCode);
            }
        }

        public bool Poll(int microSeconds, SelectMode mode)
        {
            return ProxyClient.Poll(microSeconds, mode);
        }

        public LinuxError Bind(IPEndPoint localEndPoint)
        {
            ProxyClient.RequestCommand = _isUdpSocket ? ProxyCommand.UdpAssociate : ProxyCommand.Bind;

            try
            {
                ProxyClient.Bind(localEndPoint);
            }
            catch (ProxyException exception)
            {
                Logger.Error?.Print(
                    LogClass.ServiceBsd,
                    $"Request for {ProxyClient.RequestCommand} command failed: {exception}"
                );

                return ToLinuxError(exception.ReplyCode);
            }
            catch (SocketException exception)
            {
                return WinSockHelper.ConvertError((WsaError)exception.ErrorCode);
            }

            return LinuxError.SUCCESS;
        }

        public LinuxError Connect(IPEndPoint remoteEndPoint)
        {
            ProxyClient.RequestCommand = ProxyCommand.Connect;

            try
            {
                ProxyClient.Connect(remoteEndPoint.Address, remoteEndPoint.Port);
            }
            catch (ProxyException exception)
            {
                Logger.Error?.Print(
                    LogClass.ServiceBsd,
                    $"Request for {ProxyClient.RequestCommand} command failed: {exception}"
                );

                return ToLinuxError(exception.ReplyCode);
            }
            catch (SocketException exception)
            {
                return WinSockHelper.ConvertError((WsaError)exception.ErrorCode);
            }

            return LinuxError.SUCCESS;
        }

        public LinuxError Listen(int backlog)
        {
            // NOTE: Only one client can connect with the default SOCKS5 commands.
            if (ProxyClient.RequestCommand != ProxyCommand.Bind)
            {
                return LinuxError.EOPNOTSUPP;
            }

            return LinuxError.SUCCESS;
        }

        public LinuxError Accept(out ISocket newSocket)
        {
            newSocket = null;

            if (ProxyClient.RequestCommand != ProxyCommand.Bind)
            {
                return LinuxError.EOPNOTSUPP;
            }

            // NOTE: Only one client can connect with the default SOCKS5 commands.
            if (_acceptedConnection)
            {
                return LinuxError.EOPNOTSUPP;
            }

            try
            {
                SocksClient newProxyClient = ProxyClient.Accept();
                newSocket = new ManagedProxySocket(newProxyClient);
            }
            catch (ProxyException exception)
            {
                Logger.Error?.Print(
                    LogClass.ServiceBsd,
                    $"Failed to accept client connection: {exception}"
                );

                return ToLinuxError(exception.ReplyCode);
            }
            catch (SocketException exception)
            {
                return WinSockHelper.ConvertError((WsaError)exception.ErrorCode);
            }

            return LinuxError.SUCCESS;
        }

        public void Disconnect()
        {
            ProxyClient.Disconnect();
            ProxyClient.RequestCommand = 0;
        }

        public LinuxError Shutdown(BsdSocketShutdownFlags how)
        {
            try
            {
                ProxyClient.Shutdown((SocketShutdown)how);

                return LinuxError.SUCCESS;
            }
            catch (SocketException exception)
            {
                return WinSockHelper.ConvertError((WsaError)exception.ErrorCode);
            }
        }

        public void Close()
        {
            ProxyClient.Close();
            ProxyClient.RequestCommand = 0;
        }
    }
}
