using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.Utilities;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Ryujinx.HLE.HOS.Services.Bsd
{
    internal class IClient : IpcService
    {

        private static Dictionary<WSAError, LinuxError> _errorMap = new Dictionary<WSAError, LinuxError>
        {
            // WSAEINTR
            {WSAError.Wsaeintr,           LinuxError.Eintr},
            // WSAEWOULDBLOCK
            {WSAError.Wsaewouldblock,     LinuxError.Ewouldblock},
            // WSAEINPROGRESS
            {WSAError.Wsaeinprogress,     LinuxError.Einprogress},
            // WSAEALREADY
            {WSAError.Wsaealready,        LinuxError.Ealready},
            // WSAENOTSOCK
            {WSAError.Wsaenotsock,        LinuxError.Enotsock},
            // WSAEDESTADDRREQ
            {WSAError.Wsaedestaddrreq,    LinuxError.Edestaddrreq},
            // WSAEMSGSIZE
            {WSAError.Wsaemsgsize,        LinuxError.Emsgsize},
            // WSAEPROTOTYPE
            {WSAError.Wsaeprototype,      LinuxError.Eprototype},
            // WSAENOPROTOOPT
            {WSAError.Wsaenoprotoopt,     LinuxError.Enoprotoopt},
            // WSAEPROTONOSUPPORT
            {WSAError.Wsaeprotonosupport, LinuxError.Eprotonosupport},
            // WSAESOCKTNOSUPPORT
            {WSAError.Wsaesocktnosupport, LinuxError.Esocktnosupport},
            // WSAEOPNOTSUPP
            {WSAError.Wsaeopnotsupp,      LinuxError.Eopnotsupp},
            // WSAEPFNOSUPPORT
            {WSAError.Wsaepfnosupport,    LinuxError.Epfnosupport},
            // WSAEAFNOSUPPORT
            {WSAError.Wsaeafnosupport,    LinuxError.Eafnosupport},
            // WSAEADDRINUSE
            {WSAError.Wsaeaddrinuse,      LinuxError.Eaddrinuse},
            // WSAEADDRNOTAVAIL
            {WSAError.Wsaeaddrnotavail,   LinuxError.Eaddrnotavail},
            // WSAENETDOWN
            {WSAError.Wsaenetdown,        LinuxError.Enetdown},
            // WSAENETUNREACH
            {WSAError.Wsaenetunreach,     LinuxError.Enetunreach},
            // WSAENETRESET
            {WSAError.Wsaenetreset,       LinuxError.Enetreset},
            // WSAECONNABORTED
            {WSAError.Wsaeconnaborted,    LinuxError.Econnaborted},
            // WSAECONNRESET
            {WSAError.Wsaeconnreset,      LinuxError.Econnreset},
            // WSAENOBUFS
            {WSAError.Wsaenobufs,         LinuxError.Enobufs},
            // WSAEISCONN
            {WSAError.Wsaeisconn,         LinuxError.Eisconn},
            // WSAENOTCONN
            {WSAError.Wsaenotconn,        LinuxError.Enotconn},
            // WSAESHUTDOWN
            {WSAError.Wsaeshutdown,       LinuxError.Eshutdown},
            // WSAETOOMANYREFS
            {WSAError.Wsaetoomanyrefs,    LinuxError.Etoomanyrefs},
            // WSAETIMEDOUT
            {WSAError.Wsaetimedout,       LinuxError.Etimedout},
            // WSAECONNREFUSED
            {WSAError.Wsaeconnrefused,    LinuxError.Econnrefused},
            // WSAELOOP
            {WSAError.Wsaeloop,           LinuxError.Eloop},
            // WSAENAMETOOLONG
            {WSAError.Wsaenametoolong,    LinuxError.Enametoolong},
            // WSAEHOSTDOWN
            {WSAError.Wsaehostdown,       LinuxError.Ehostdown},
            // WSAEHOSTUNREACH
            {WSAError.Wsaehostunreach,    LinuxError.Ehostunreach},
            // WSAENOTEMPTY
            {WSAError.Wsaenotempty,       LinuxError.Enotempty},
            // WSAEUSERS
            {WSAError.Wsaeusers,          LinuxError.Eusers},
            // WSAEDQUOT
            {WSAError.Wsaedquot,          LinuxError.Edquot},
            // WSAESTALE
            {WSAError.Wsaestale,          LinuxError.Estale},
            // WSAEREMOTE
            {WSAError.Wsaeremote,         LinuxError.Eremote},
            // WSAEINVAL
            {WSAError.Wsaeinval,          LinuxError.Einval},
            // WSAEFAULT
            {WSAError.Wsaefault,          LinuxError.Efault},
            // NOERROR
            {0, 0}
        };

        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        private bool _isPrivileged;

        private List<BsdSocket> _sockets = new List<BsdSocket>();

        public IClient(bool isPrivileged)
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0,  RegisterClient     },
                { 1,  StartMonitoring    },
                { 2,  Socket             },
                { 3,  SocketExempt       },
                { 4,  Open               },
                { 5,  Select             },
                { 6,  Poll               },
                { 7,  Sysctl             },
                { 8,  Recv               },
                { 9,  RecvFrom           },
                { 10, Send               },
                { 11, SendTo             },
                { 12, Accept             },
                { 13, Bind               },
                { 14, Connect            },
                { 15, GetPeerName        },
                { 16, GetSockName        },
                { 17, GetSockOpt         },
                { 18, Listen             },
                { 19, Ioctl              },
                { 20, Fcntl              },
                { 21, SetSockOpt         },
                { 22, Shutdown           },
                { 23, ShutdownAllSockets },
                { 24, Write              },
                { 25, Read               },
                { 26, Close              },
                { 27, DuplicateSocket    },
            };

            _isPrivileged = isPrivileged;
        }

        private LinuxError ConvertError(WSAError errorCode)
        {
            LinuxError errno;

            if (!_errorMap.TryGetValue(errorCode, out errno))
            {
                errno = (LinuxError)errorCode;
            }

            return errno;
        }

        private long WriteWinSock2Error(ServiceCtx context, WSAError errorCode)
        {
            return WriteBsdResult(context, -1, ConvertError(errorCode));
        }

        private long WriteBsdResult(ServiceCtx context, int result, LinuxError errorCode = 0)
        {
            if (errorCode != LinuxError.Success)
            {
                result = -1;
            }

            context.ResponseData.Write(result);
            context.ResponseData.Write((int)errorCode);

            return 0;
        }

        private BsdSocket RetrieveSocket(int socketFd)
        {
            if (socketFd >= 0 && _sockets.Count > socketFd)
            {
                return _sockets[socketFd];
            }

            return null;
        }

        private LinuxError SetResultErrno(Socket socket, int result)
        {
            return result == 0 && !socket.Blocking ? LinuxError.Ewouldblock : LinuxError.Success;
        }

        private AddressFamily ConvertFromBsd(int domain)
        {
            if (domain == 2)
            {
                return AddressFamily.InterNetwork;
            }

            // FIXME: AF_ROUTE ignored, is that really needed?
            return AddressFamily.Unknown;
        }

        private long SocketInternal(ServiceCtx context, bool exempt)
        {
            AddressFamily domain   = (AddressFamily)context.RequestData.ReadInt32();
            SocketType    type     = (SocketType)context.RequestData.ReadInt32();
            ProtocolType  protocol = (ProtocolType)context.RequestData.ReadInt32();

            if (domain == AddressFamily.Unknown)
            {
                return WriteBsdResult(context, -1, LinuxError.Eprotonosupport);
            }
            else if ((type == SocketType.Seqpacket || type == SocketType.Raw) && !_isPrivileged)
            {
                if (domain != AddressFamily.InterNetwork || type != SocketType.Raw || protocol != ProtocolType.Icmp)
                {
                    return WriteBsdResult(context, -1, LinuxError.Enoent);
                }
            }

            BsdSocket newBsdSocket = new BsdSocket
            {
                Family   = (int)domain,
                Type     = (int)type,
                Protocol = (int)protocol,
                Handle   = new Socket(domain, type, protocol)
            };

            _sockets.Add(newBsdSocket);

            if (exempt)
            {
                newBsdSocket.Handle.Disconnect(true);
            }

            return WriteBsdResult(context, _sockets.Count - 1);
        }

        private IPEndPoint ParseSockAddr(ServiceCtx context, long bufferPosition, long bufferSize)
        {
            int size   = context.Memory.ReadByte(bufferPosition);
            int family = context.Memory.ReadByte(bufferPosition + 1);
            int port   = EndianSwap.Swap16(context.Memory.ReadUInt16(bufferPosition + 2));

            byte[] rawIp = context.Memory.ReadBytes(bufferPosition + 4, 4);

            return new IPEndPoint(new IPAddress(rawIp), port);
        }

        private void WriteSockAddr(ServiceCtx context, long bufferPosition, IPEndPoint endPoint)
        {
            context.Memory.WriteByte(bufferPosition, 0);
            context.Memory.WriteByte(bufferPosition + 1, (byte)endPoint.AddressFamily);
            context.Memory.WriteUInt16(bufferPosition + 2, EndianSwap.Swap16((ushort)endPoint.Port));
            context.Memory.WriteBytes(bufferPosition + 4, endPoint.Address.GetAddressBytes());
        }

        private void WriteSockAddr(ServiceCtx context, long bufferPosition, BsdSocket socket, bool isRemote)
        {
            IPEndPoint endPoint = (isRemote ? socket.Handle.RemoteEndPoint : socket.Handle.LocalEndPoint) as IPEndPoint;

            WriteSockAddr(context, bufferPosition, endPoint);
        }

        // Initialize(nn::socket::BsdBufferConfig config, u64 pid, u64 transferMemorySize, KObject<copy, transfer_memory>, pid) -> u32 bsd_errno
        public long RegisterClient(ServiceCtx context)
        {
            /*
            typedef struct  {
                u32 version;                // Observed 1 on 2.0 LibAppletWeb, 2 on 3.0.
                u32 tcp_tx_buf_size;        // Size of the TCP transfer (send) buffer (initial or fixed).
                u32 tcp_rx_buf_size;        // Size of the TCP recieve buffer (initial or fixed).
                u32 tcp_tx_buf_max_size;    // Maximum size of the TCP transfer (send) buffer. If it is 0, the size of the buffer is fixed to its initial value.
                u32 tcp_rx_buf_max_size;    // Maximum size of the TCP receive buffer. If it is 0, the size of the buffer is fixed to its initial value.
                u32 udp_tx_buf_size;        // Size of the UDP transfer (send) buffer (typically 0x2400 bytes).
                u32 udp_rx_buf_size;        // Size of the UDP receive buffer (typically 0xA500 bytes).
                u32 sb_efficiency;          // Number of buffers for each socket (standard values range from 1 to 8).
            } BsdBufferConfig;
            */

            // bsd_error
            context.ResponseData.Write(0);

            Logger.PrintStub(LogClass.ServiceBsd, "Stubbed.");

            return 0;
        }

        // StartMonitoring(u64, pid)
        public long StartMonitoring(ServiceCtx context)
        {
            ulong unknown0 = context.RequestData.ReadUInt64();

            Logger.PrintStub(LogClass.ServiceBsd, $"Stubbed. Unknown0: {unknown0}");

            return 0;
        }

        // Socket(u32 domain, u32 type, u32 protocol) -> (i32 ret, u32 bsd_errno)
        public long Socket(ServiceCtx context)
        {
            return SocketInternal(context, false);
        }

        // SocketExempt(u32 domain, u32 type, u32 protocol) -> (i32 ret, u32 bsd_errno)
        public long SocketExempt(ServiceCtx context)
        {
            return SocketInternal(context, true);
        }

        // Open(u32 flags, array<unknown, 0x21> path) -> (i32 ret, u32 bsd_errno)
        public long Open(ServiceCtx context)
        {
            (long bufferPosition, long bufferSize) = context.Request.GetBufferType0X21();

            int flags = context.RequestData.ReadInt32();

            byte[] rawPath = context.Memory.ReadBytes(bufferPosition, bufferSize);
            string path    = Encoding.ASCII.GetString(rawPath);

            WriteBsdResult(context, -1, LinuxError.Eopnotsupp);

            Logger.PrintStub(LogClass.ServiceBsd, $"Stubbed. Path: {path} - " +
                                                  $"Flags: {flags}");

            return 0;
        }

        // Select(u32 nfds, nn::socket::timeout timeout, buffer<nn::socket::fd_set, 0x21, 0> readfds_in, buffer<nn::socket::fd_set, 0x21, 0> writefds_in, buffer<nn::socket::fd_set, 0x21, 0> errorfds_in) -> (i32 ret, u32 bsd_errno, buffer<nn::socket::fd_set, 0x22, 0> readfds_out, buffer<nn::socket::fd_set, 0x22, 0> writefds_out, buffer<nn::socket::fd_set, 0x22, 0> errorfds_out)
        public long Select(ServiceCtx context)
        {
            WriteBsdResult(context, -1, LinuxError.Eopnotsupp);

            Logger.PrintStub(LogClass.ServiceBsd, $"Stubbed.");

            return 0;
        }

        // Poll(u32 nfds, u32 timeout, buffer<unknown, 0x21, 0> fds) -> (i32 ret, u32 bsd_errno, buffer<unknown, 0x22, 0>)
        public long Poll(ServiceCtx context)
        {
            int fdsCount = context.RequestData.ReadInt32();
            int timeout  = context.RequestData.ReadInt32();

            (long bufferPosition, long bufferSize) = context.Request.GetBufferType0X21();


            if (timeout < -1 || fdsCount < 0 || fdsCount * 8 > bufferSize)
            {
                return WriteBsdResult(context, -1, LinuxError.Einval);
            }

            PollEvent[] events = new PollEvent[fdsCount];

            for (int i = 0; i < fdsCount; i++)
            {
                int socketFd = context.Memory.ReadInt32(bufferPosition + i * 8);

                BsdSocket socket = RetrieveSocket(socketFd);

                if (socket == null)
                {
                    return WriteBsdResult(context, -1, LinuxError.Ebadf);
                }

                PollEvent.EventTypeMask inputEvents  = (PollEvent.EventTypeMask)context.Memory.ReadInt16(bufferPosition + i * 8 + 4);
                PollEvent.EventTypeMask outputEvents = (PollEvent.EventTypeMask)context.Memory.ReadInt16(bufferPosition + i * 8 + 6);

                events[i] = new PollEvent(socketFd, socket, inputEvents, outputEvents);
            }

            List<Socket> readEvents  = new List<Socket>();
            List<Socket> writeEvents = new List<Socket>();
            List<Socket> errorEvents = new List<Socket>();

            foreach (PollEvent Event in events)
            {
                bool isValidEvent = false;

                if ((Event.InputEvents & PollEvent.EventTypeMask.Input) != 0)
                {
                    readEvents.Add(Event.Socket.Handle);
                    errorEvents.Add(Event.Socket.Handle);

                    isValidEvent = true;
                }

                if ((Event.InputEvents & PollEvent.EventTypeMask.UrgentInput) != 0)
                {
                    readEvents.Add(Event.Socket.Handle);
                    errorEvents.Add(Event.Socket.Handle);

                    isValidEvent = true;
                }

                if ((Event.InputEvents & PollEvent.EventTypeMask.Output) != 0)
                {
                    writeEvents.Add(Event.Socket.Handle);
                    errorEvents.Add(Event.Socket.Handle);

                    isValidEvent = true;
                }

                if ((Event.InputEvents & PollEvent.EventTypeMask.Error) != 0)
                {
                    errorEvents.Add(Event.Socket.Handle);
                    isValidEvent = true;
                }

                if (!isValidEvent)
                {
                    Logger.PrintWarning(LogClass.ServiceBsd, $"Unsupported Poll input event type: {Event.InputEvents}");
                    return WriteBsdResult(context, -1, LinuxError.Einval);
                }
            }

            try
            {
                System.Net.Sockets.Socket.Select(readEvents, writeEvents, errorEvents, timeout);
            }
            catch (SocketException exception)
            {
                return WriteWinSock2Error(context, (WSAError)exception.ErrorCode);
            }

            for (int i = 0; i < fdsCount; i++)
            {
                PollEvent Event = events[i];
                context.Memory.WriteInt32(bufferPosition + i * 8, Event.SocketFd);
                context.Memory.WriteInt16(bufferPosition + i * 8 + 4, (short)Event.InputEvents);

                PollEvent.EventTypeMask outputEvents = 0;

                Socket socket = Event.Socket.Handle;

                if (errorEvents.Contains(socket))
                {
                    outputEvents |= PollEvent.EventTypeMask.Error;

                    if (!socket.Connected || !socket.IsBound)
                    {
                        outputEvents |= PollEvent.EventTypeMask.Disconnected;
                    }
                }

                if (readEvents.Contains(socket))
                {
                    if ((Event.InputEvents & PollEvent.EventTypeMask.Input) != 0)
                    {
                        outputEvents |= PollEvent.EventTypeMask.Input;
                    }
                }

                if (writeEvents.Contains(socket))
                {
                    outputEvents |= PollEvent.EventTypeMask.Output;
                }

                context.Memory.WriteInt16(bufferPosition + i * 8 + 6, (short)outputEvents);
            }

            return WriteBsdResult(context, readEvents.Count + writeEvents.Count + errorEvents.Count, LinuxError.Success);
        }

        // Sysctl(buffer<unknown, 0x21, 0>, buffer<unknown, 0x21, 0>) -> (i32 ret, u32 bsd_errno, u32, buffer<unknown, 0x22, 0>)
        public long Sysctl(ServiceCtx context)
        {
            WriteBsdResult(context, -1, LinuxError.Eopnotsupp);

            Logger.PrintStub(LogClass.ServiceBsd, $"Stubbed.");

            return 0;
        }

        // Recv(u32 socket, u32 flags) -> (i32 ret, u32 bsd_errno, array<i8, 0x22> message)
        public long Recv(ServiceCtx context)
        {
            int         socketFd    = context.RequestData.ReadInt32();
            SocketFlags socketFlags = (SocketFlags)context.RequestData.ReadInt32();

            (long receivePosition, long receiveLength) = context.Request.GetBufferType0X22();

            LinuxError errno  = LinuxError.Ebadf;
            BsdSocket  socket = RetrieveSocket(socketFd);
            int        result = -1;

            if (socket != null)
            {
                if (socketFlags != SocketFlags.None && (socketFlags & SocketFlags.OutOfBand) == 0
                    && (socketFlags & SocketFlags.Peek) == 0)
                {
                    Logger.PrintWarning(LogClass.ServiceBsd, $"Unsupported Recv flags: {socketFlags}");
                    return WriteBsdResult(context, -1, LinuxError.Eopnotsupp);
                }

                byte[] receivedBuffer = new byte[receiveLength];

                try
                {
                    result = socket.Handle.Receive(receivedBuffer, socketFlags);
                    errno  = SetResultErrno(socket.Handle, result);

                    context.Memory.WriteBytes(receivePosition, receivedBuffer);
                }
                catch (SocketException exception)
                {
                    errno = ConvertError((WSAError)exception.ErrorCode);
                }
            }

            return WriteBsdResult(context, result, errno);
        }

        // RecvFrom(u32 sock, u32 flags) -> (i32 ret, u32 bsd_errno, u32 addrlen, buffer<i8, 0x22, 0> message, buffer<nn::socket::sockaddr_in, 0x22, 0x10>)
        public long RecvFrom(ServiceCtx context)
        {
            int         socketFd    = context.RequestData.ReadInt32();
            SocketFlags socketFlags = (SocketFlags)context.RequestData.ReadInt32();

            (long receivePosition,     long receiveLength)   = context.Request.GetBufferType0X22();
            (long sockAddrInPosition,  long sockAddrInSize)  = context.Request.GetBufferType0X21();
            (long sockAddrOutPosition, long sockAddrOutSize) = context.Request.GetBufferType0X22(1);

            LinuxError errno  = LinuxError.Ebadf;
            BsdSocket  socket = RetrieveSocket(socketFd);
            int        result = -1;

            if (socket != null)
            {
                if (socketFlags != SocketFlags.None && (socketFlags & SocketFlags.OutOfBand) == 0
                    && (socketFlags & SocketFlags.Peek) == 0)
                {
                    Logger.PrintWarning(LogClass.ServiceBsd, $"Unsupported Recv flags: {socketFlags}");

                    return WriteBsdResult(context, -1, LinuxError.Eopnotsupp);
                }

                byte[]   receivedBuffer = new byte[receiveLength];
                EndPoint endPoint       = ParseSockAddr(context, sockAddrInPosition, sockAddrInSize);

                try
                {
                    result = socket.Handle.ReceiveFrom(receivedBuffer, receivedBuffer.Length, socketFlags, ref endPoint);
                    errno  = SetResultErrno(socket.Handle, result);

                    context.Memory.WriteBytes(receivePosition, receivedBuffer);
                    WriteSockAddr(context, sockAddrOutPosition, (IPEndPoint)endPoint);
                }
                catch (SocketException exception)
                {
                    errno = ConvertError((WSAError)exception.ErrorCode);
                }
            }

            return WriteBsdResult(context, result, errno);
        }

        // Send(u32 socket, u32 flags, buffer<i8, 0x21, 0>) -> (i32 ret, u32 bsd_errno)
        public long Send(ServiceCtx context)
        {
            int         socketFd    = context.RequestData.ReadInt32();
            SocketFlags socketFlags = (SocketFlags)context.RequestData.ReadInt32();

            (long sendPosition, long sendSize) = context.Request.GetBufferType0X21();

            LinuxError errno  = LinuxError.Ebadf;
            BsdSocket  socket = RetrieveSocket(socketFd);
            int        result = -1;

            if (socket != null)
            {
                if (socketFlags != SocketFlags.None && socketFlags != SocketFlags.OutOfBand
                    && socketFlags != SocketFlags.Peek && socketFlags != SocketFlags.DontRoute)
                {
                    Logger.PrintWarning(LogClass.ServiceBsd, $"Unsupported Send flags: {socketFlags}");

                    return WriteBsdResult(context, -1, LinuxError.Eopnotsupp);
                }

                byte[] sendBuffer = context.Memory.ReadBytes(sendPosition, sendSize);

                try
                {
                    result = socket.Handle.Send(sendBuffer, socketFlags);
                    errno  = SetResultErrno(socket.Handle, result);
                }
                catch (SocketException exception)
                {
                    errno = ConvertError((WSAError)exception.ErrorCode);
                }

            }

            return WriteBsdResult(context, result, errno);
        }

        // SendTo(u32 socket, u32 flags, buffer<i8, 0x21, 0>, buffer<nn::socket::sockaddr_in, 0x21, 0x10>) -> (i32 ret, u32 bsd_errno)
        public long SendTo(ServiceCtx context)
        {
            int         socketFd    = context.RequestData.ReadInt32();
            SocketFlags socketFlags = (SocketFlags)context.RequestData.ReadInt32();

            (long sendPosition,   long sendSize)   = context.Request.GetBufferType0X21();
            (long bufferPosition, long bufferSize) = context.Request.GetBufferType0X21(1);

            LinuxError errno  = LinuxError.Ebadf;
            BsdSocket  socket = RetrieveSocket(socketFd);
            int        result = -1;

            if (socket != null)
            {
                if (socketFlags != SocketFlags.None && socketFlags != SocketFlags.OutOfBand
                    && socketFlags != SocketFlags.Peek && socketFlags != SocketFlags.DontRoute)
                {
                    Logger.PrintWarning(LogClass.ServiceBsd, $"Unsupported Send flags: {socketFlags}");

                    return WriteBsdResult(context, -1, LinuxError.Eopnotsupp);
                }

                byte[]   sendBuffer = context.Memory.ReadBytes(sendPosition, sendSize);
                EndPoint endPoint   = ParseSockAddr(context, bufferPosition, bufferSize);

                try
                {
                    result = socket.Handle.SendTo(sendBuffer, sendBuffer.Length, socketFlags, endPoint);
                    errno  = SetResultErrno(socket.Handle, result);
                }
                catch (SocketException exception)
                {
                    errno = ConvertError((WSAError)exception.ErrorCode);
                }

            }

            return WriteBsdResult(context, result, errno);
        }

        // Accept(u32 socket) -> (i32 ret, u32 bsd_errno, u32 addrlen, buffer<nn::socket::sockaddr_in, 0x22, 0x10> addr)
        public long Accept(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();

            (long bufferPos, long bufferSize) = context.Request.GetBufferType0X22();

            LinuxError errno  = LinuxError.Ebadf;
            BsdSocket  socket = RetrieveSocket(socketFd);

            if (socket != null)
            {
                errno = LinuxError.Success;

                Socket newSocket = null;

                try
                {
                    newSocket = socket.Handle.Accept();
                }
                catch (SocketException exception)
                {
                    errno = ConvertError((WSAError)exception.ErrorCode);
                }

                if (newSocket == null && errno == LinuxError.Success)
                {
                    errno = LinuxError.Ewouldblock;
                }
                else if (errno == LinuxError.Success)
                {
                    BsdSocket newBsdSocket = new BsdSocket
                    {
                        Family   = (int)newSocket.AddressFamily,
                        Type     = (int)newSocket.SocketType,
                        Protocol = (int)newSocket.ProtocolType,
                        Handle   = newSocket,
                    };

                    _sockets.Add(newBsdSocket);

                    WriteSockAddr(context, bufferPos, newBsdSocket, true);

                    WriteBsdResult(context, _sockets.Count - 1, errno);

                    context.ResponseData.Write(0x10);

                    return 0;
                }
            }

            return WriteBsdResult(context, -1, errno);
        }

        // Bind(u32 socket, buffer<nn::socket::sockaddr_in, 0x21, 0x10> addr) -> (i32 ret, u32 bsd_errno)
        public long Bind(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();

            (long bufferPos, long bufferSize) = context.Request.GetBufferType0X21();

            LinuxError errno  = LinuxError.Ebadf;
            BsdSocket  socket = RetrieveSocket(socketFd);

            if (socket != null)
            {
                errno = LinuxError.Success;

                try
                {
                    IPEndPoint endPoint = ParseSockAddr(context, bufferPos, bufferSize);

                    socket.Handle.Bind(endPoint);
                }
                catch (SocketException exception)
                {
                    errno = ConvertError((WSAError)exception.ErrorCode);
                }
            }

            return WriteBsdResult(context, 0, errno);
        }

        // Connect(u32 socket, buffer<nn::socket::sockaddr_in, 0x21, 0x10>) -> (i32 ret, u32 bsd_errno)
        public long Connect(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();

            (long bufferPos, long bufferSize) = context.Request.GetBufferType0X21();

            LinuxError errno  = LinuxError.Ebadf;
            BsdSocket  socket = RetrieveSocket(socketFd);

            if (socket != null)
            {
                errno = LinuxError.Success;
                try
                {
                    IPEndPoint endPoint = ParseSockAddr(context, bufferPos, bufferSize);

                    socket.Handle.Connect(endPoint);
                }
                catch (SocketException exception)
                {
                    errno = ConvertError((WSAError)exception.ErrorCode);
                }
            }

            return WriteBsdResult(context, 0, errno);
        }

        // GetPeerName(u32 socket) -> (i32 ret, u32 bsd_errno, u32 addrlen, buffer<nn::socket::sockaddr_in, 0x22, 0x10> addr)
        public long GetPeerName(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();

            (long bufferPos, long bufferSize) = context.Request.GetBufferType0X22();

            LinuxError  errno  = LinuxError.Ebadf;
            BsdSocket socket = RetrieveSocket(socketFd);

            if (socket != null)
            {
                errno = LinuxError.Success;

                WriteSockAddr(context, bufferPos, socket, true);
                WriteBsdResult(context, 0, errno);
                context.ResponseData.Write(0x10);
            }

            return WriteBsdResult(context, 0, errno);
        }

        // GetSockName(u32 socket) -> (i32 ret, u32 bsd_errno, u32 addrlen, buffer<nn::socket::sockaddr_in, 0x22, 0x10> addr)
        public long GetSockName(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();

            (long bufferPos, long bufferSize) = context.Request.GetBufferType0X22();

            LinuxError errno  = LinuxError.Ebadf;
            BsdSocket  socket = RetrieveSocket(socketFd);

            if (socket != null)
            {
                errno = LinuxError.Success;

                WriteSockAddr(context, bufferPos, socket, false);
                WriteBsdResult(context, 0, errno);
                context.ResponseData.Write(0x10);
            }

            return WriteBsdResult(context, 0, errno);
        }

        // GetSockOpt(u32 socket, u32 level, u32 option_name) -> (i32 ret, u32 bsd_errno, u32, buffer<unknown, 0x22, 0>)
        public long GetSockOpt(ServiceCtx context)
        {
            int socketFd   = context.RequestData.ReadInt32();
            int level      = context.RequestData.ReadInt32();
            int optionName = context.RequestData.ReadInt32();

            (long bufferPosition, long bufferSize) = context.Request.GetBufferType0X22();

            LinuxError errno  = LinuxError.Ebadf;
            BsdSocket  socket = RetrieveSocket(socketFd);

            if (socket != null)
            {
                errno = LinuxError.Enoprotoopt;

                if (level == 0xFFFF)
                {
                    errno = HandleGetSocketOption(context, socket, (SocketOptionName)optionName, bufferPosition, bufferSize);
                }
                else
                {
                    Logger.PrintWarning(LogClass.ServiceBsd, $"Unsupported GetSockOpt Level: {(SocketOptionLevel)level}");
                }
            }

            return WriteBsdResult(context, 0, errno);
        }

        // Listen(u32 socket, u32 backlog) -> (i32 ret, u32 bsd_errno)
        public long Listen(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();
            int backlog  = context.RequestData.ReadInt32();

            LinuxError errno  = LinuxError.Ebadf;
            BsdSocket  socket = RetrieveSocket(socketFd);

            if (socket != null)
            {
                errno = LinuxError.Success;

                try
                {
                    socket.Handle.Listen(backlog);
                }
                catch (SocketException exception)
                {
                    errno = ConvertError((WSAError)exception.ErrorCode);
                }
            }

            return WriteBsdResult(context, 0, errno);
        }

        // Ioctl(u32 fd, u32 request, u32 bufcount, buffer<unknown, 0x21, 0>, buffer<unknown, 0x21, 0>, buffer<unknown, 0x21, 0>, buffer<unknown, 0x21, 0>) -> (i32 ret, u32 bsd_errno, buffer<unknown, 0x22, 0>, buffer<unknown, 0x22, 0>, buffer<unknown, 0x22, 0>, buffer<unknown, 0x22, 0>)
        public long Ioctl(ServiceCtx context)
        {
            int      socketFd    = context.RequestData.ReadInt32();
            BsdIoctl cmd         = (BsdIoctl)context.RequestData.ReadInt32();
            int      bufferCount = context.RequestData.ReadInt32();

            LinuxError errno  = LinuxError.Ebadf;
            BsdSocket  socket = RetrieveSocket(socketFd);

            if (socket != null)
            {
                switch (cmd)
                {
                    case BsdIoctl.AtMark:
                        errno = LinuxError.Success;

                        (long bufferPosition, long bufferSize) = context.Request.GetBufferType0X22();

                        // FIXME: OOB not implemented.
                        context.Memory.WriteInt32(bufferPosition, 0);
                        break;

                    default:
                        errno = LinuxError.Eopnotsupp;

                        Logger.PrintWarning(LogClass.ServiceBsd, $"Unsupported Ioctl Cmd: {cmd}");
                        break;
                }
            }

            return WriteBsdResult(context, 0, errno);
        }

        // Fcntl(u32 socket, u32 cmd, u32 arg) -> (i32 ret, u32 bsd_errno)
        public long Fcntl(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();
            int cmd      = context.RequestData.ReadInt32();
            int arg      = context.RequestData.ReadInt32();

            int        result = 0;
            LinuxError errno  = LinuxError.Ebadf;
            BsdSocket  socket = RetrieveSocket(socketFd);

            if (socket != null)
            {
                errno = LinuxError.Success;

                if (cmd == 0x3)
                {
                    result = !socket.Handle.Blocking ? 0x800 : 0;
                }
                else if (cmd == 0x4 && arg == 0x800)
                {
                    socket.Handle.Blocking = false;
                    result = 0;
                }
                else
                {
                    errno = LinuxError.Eopnotsupp;
                }
            }

            return WriteBsdResult(context, result, errno);
        }

        private LinuxError HandleGetSocketOption(ServiceCtx context, BsdSocket socket, SocketOptionName optionName, long optionValuePosition, long optionValueSize)
        {
            try
            {
                byte[] optionValue = new byte[optionValueSize];

                switch (optionName)
                {
                    case SocketOptionName.Broadcast:
                    case SocketOptionName.DontLinger:
                    case SocketOptionName.Debug:
                    case SocketOptionName.Error:
                    case SocketOptionName.KeepAlive:
                    case SocketOptionName.OutOfBandInline:
                    case SocketOptionName.ReceiveBuffer:
                    case SocketOptionName.ReceiveTimeout:
                    case SocketOptionName.SendBuffer:
                    case SocketOptionName.SendTimeout:
                    case SocketOptionName.Type:
                    case SocketOptionName.Linger:
                        socket.Handle.GetSocketOption(SocketOptionLevel.Socket, optionName, optionValue);
                        context.Memory.WriteBytes(optionValuePosition, optionValue);

                        return LinuxError.Success;

                    case (SocketOptionName)0x200:
                        socket.Handle.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optionValue);
                        context.Memory.WriteBytes(optionValuePosition, optionValue);

                        return LinuxError.Success;

                    default:
                        Logger.PrintWarning(LogClass.ServiceBsd, $"Unsupported SetSockOpt OptionName: {optionName}");

                        return LinuxError.Eopnotsupp;
                }
            }
            catch (SocketException exception)
            {
                return ConvertError((WSAError)exception.ErrorCode);
            }
        }

        private LinuxError HandleSetSocketOption(ServiceCtx context, BsdSocket socket, SocketOptionName optionName, long optionValuePosition, long optionValueSize)
        {
            try
            {
                switch (optionName)
                {
                    case SocketOptionName.Broadcast:
                    case SocketOptionName.DontLinger:
                    case SocketOptionName.Debug:
                    case SocketOptionName.Error:
                    case SocketOptionName.KeepAlive:
                    case SocketOptionName.OutOfBandInline:
                    case SocketOptionName.ReceiveBuffer:
                    case SocketOptionName.ReceiveTimeout:
                    case SocketOptionName.SendBuffer:
                    case SocketOptionName.SendTimeout:
                    case SocketOptionName.Type:
                    case SocketOptionName.ReuseAddress:
                        socket.Handle.SetSocketOption(SocketOptionLevel.Socket, optionName, context.Memory.ReadInt32(optionValuePosition));

                        return LinuxError.Success;

                    case (SocketOptionName)0x200:
                        socket.Handle.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, context.Memory.ReadInt32(optionValuePosition));

                        return LinuxError.Success;

                    case SocketOptionName.Linger:
                        socket.Handle.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger,
                            new LingerOption(context.Memory.ReadInt32(optionValuePosition) != 0, context.Memory.ReadInt32(optionValuePosition + 4)));

                        return LinuxError.Success;

                    default:
                        Logger.PrintWarning(LogClass.ServiceBsd, $"Unsupported SetSockOpt OptionName: {optionName}");

                        return LinuxError.Eopnotsupp;
                }
            }
            catch (SocketException exception)
            {
                return ConvertError((WSAError)exception.ErrorCode);
            }
        }

        // SetSockOpt(u32 socket, u32 level, u32 option_name, buffer<unknown, 0x21, 0> option_value) -> (i32 ret, u32 bsd_errno)
        public long SetSockOpt(ServiceCtx context)
        {
            int socketFd   = context.RequestData.ReadInt32();
            int level      = context.RequestData.ReadInt32();
            int optionName = context.RequestData.ReadInt32();

            (long bufferPos, long bufferSize) = context.Request.GetBufferType0X21();

            LinuxError errno  = LinuxError.Ebadf;
            BsdSocket  socket = RetrieveSocket(socketFd);

            if (socket != null)
            {
                errno = LinuxError.Enoprotoopt;

                if (level == 0xFFFF)
                {
                    errno = HandleSetSocketOption(context, socket, (SocketOptionName)optionName, bufferPos, bufferSize);
                }
                else
                {
                    Logger.PrintWarning(LogClass.ServiceBsd, $"Unsupported SetSockOpt Level: {(SocketOptionLevel)level}");
                }
            }

            return WriteBsdResult(context, 0, errno);
        }

        // Shutdown(u32 socket, u32 how) -> (i32 ret, u32 bsd_errno)
        public long Shutdown(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();
            int how      = context.RequestData.ReadInt32();

            LinuxError errno  = LinuxError.Ebadf;
            BsdSocket  socket = RetrieveSocket(socketFd);

            if (socket != null)
            {
                errno = LinuxError.Einval;

                if (how >= 0 && how <= 2)
                {
                    errno = LinuxError.Success;

                    try
                    {
                        socket.Handle.Shutdown((SocketShutdown)how);
                    }
                    catch (SocketException exception)
                    {
                        errno = ConvertError((WSAError)exception.ErrorCode);
                    }
                }
            }

            return WriteBsdResult(context, 0, errno);
        }

        // ShutdownAllSockets(u32 how) -> (i32 ret, u32 bsd_errno)
        public long ShutdownAllSockets(ServiceCtx context)
        {
            int how = context.RequestData.ReadInt32();

            LinuxError errno = LinuxError.Einval;

            if (how >= 0 && how <= 2)
            {
                errno = LinuxError.Success;

                foreach (BsdSocket socket in _sockets)
                {
                    if (socket != null)
                    {
                        try
                        {
                            socket.Handle.Shutdown((SocketShutdown)how);
                        }
                        catch (SocketException exception)
                        {
                            errno = ConvertError((WSAError)exception.ErrorCode);
                            break;
                        }
                    }
                }
            }

            return WriteBsdResult(context, 0, errno);
        }

        // Write(u32 socket, buffer<i8, 0x21, 0> message) -> (i32 ret, u32 bsd_errno)
        public long Write(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();

            (long sendPosition, long sendSize) = context.Request.GetBufferType0X21();

            LinuxError errno  = LinuxError.Ebadf;
            BsdSocket  socket = RetrieveSocket(socketFd);
            int        result = -1;

            if (socket != null)
            {
                byte[] sendBuffer = context.Memory.ReadBytes(sendPosition, sendSize);

                try
                {
                    result = socket.Handle.Send(sendBuffer);
                    errno  = SetResultErrno(socket.Handle, result);
                }
                catch (SocketException exception)
                {
                    errno = ConvertError((WSAError)exception.ErrorCode);
                }
            }

            return WriteBsdResult(context, result, errno);
        }

        // Read(u32 socket) -> (i32 ret, u32 bsd_errno, buffer<i8, 0x22, 0> message)
        public long Read(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();

            (long receivePosition, long receiveLength) = context.Request.GetBufferType0X22();

            LinuxError errno  = LinuxError.Ebadf;
            BsdSocket  socket = RetrieveSocket(socketFd);
            int        result = -1;

            if (socket != null)
            {
                byte[] receivedBuffer = new byte[receiveLength];

                try
                {
                    result = socket.Handle.Receive(receivedBuffer);
                    errno  = SetResultErrno(socket.Handle, result);
                }
                catch (SocketException exception)
                {
                    errno = ConvertError((WSAError)exception.ErrorCode);
                }
            }

            return WriteBsdResult(context, result, errno);
        }

        // Close(u32 socket) -> (i32 ret, u32 bsd_errno)
        public long Close(ServiceCtx context)
        {
            int socketFd = context.RequestData.ReadInt32();

            LinuxError errno  = LinuxError.Ebadf;
            BsdSocket  socket = RetrieveSocket(socketFd);

            if (socket != null)
            {
                socket.Handle.Close();

                _sockets[socketFd] = null;

                errno = LinuxError.Success;
            }

            return WriteBsdResult(context, 0, errno);
        }

        // DuplicateSocket(u32 socket, u64 reserved) -> (i32 ret, u32 bsd_errno)
        public long DuplicateSocket(ServiceCtx context)
        {
            int   socketFd = context.RequestData.ReadInt32();
            ulong reserved = context.RequestData.ReadUInt64();

            LinuxError errno     = LinuxError.Enoent;
            int        newSockFd = -1;

            if (_isPrivileged)
            {
                errno = LinuxError.Ebadf;

                BsdSocket oldSocket = RetrieveSocket(socketFd);

                if (oldSocket != null)
                {
                    _sockets.Add(oldSocket);
                    newSockFd = _sockets.Count - 1;
                }
            }

            return WriteBsdResult(context, newSockFd, errno);
        }
    }
}
