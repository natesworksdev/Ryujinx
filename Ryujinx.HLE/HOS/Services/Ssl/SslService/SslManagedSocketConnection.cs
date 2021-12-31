using Ryujinx.HLE.HOS.Services.Sockets.Bsd;
using Ryujinx.HLE.HOS.Services.Ssl.Types;
using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;

namespace Ryujinx.HLE.HOS.Services.Ssl.SslService
{
    class SslManagedSocketConnection : ISslConnectionBase
    {
        public int SocketFd { get; }

        public ISocket Socket { get; }

        private BsdContext _bsdContext;
        private SslVersion _sslVersion;
        private SslStream _stream;
        private bool _isBlockingSocket;
        private int _previousReadTimeout;

        public SslManagedSocketConnection(BsdContext bsdContext, SslVersion sslVersion, int socketFd, ISocket socket)
        {
            _bsdContext = bsdContext;
            _sslVersion = sslVersion;

            SocketFd = socketFd;
            Socket = socket;
        }

        private void StartSslOperation()
        {
            // Save blocking state
            _isBlockingSocket = Socket.Blocking;

            // Force blocking for SslStream
            Socket.Blocking = true;
        }

        private void EndSslOperation()
        {
            // Restore blocking state
            Socket.Blocking = _isBlockingSocket;
        }

        private void StartSslReadOperation()
        {
            StartSslOperation();

            _previousReadTimeout = _stream.ReadTimeout;

            _stream.ReadTimeout = 1;
        }

        private void EndSslReadOperation()
        {
            _stream.ReadTimeout = _previousReadTimeout;

            EndSslOperation();
        }

        private static SslProtocols TranslateSslVersion(SslVersion version)
        {
            switch (version)
            {
                case SslVersion.Auto:
                case SslVersion.Auto2:
                    return SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13;
                case SslVersion.TlsV10:
                    return SslProtocols.Tls;
                case SslVersion.TlsV11:
                    return SslProtocols.Tls11;
                case SslVersion.TlsV12:
                    return SslProtocols.Tls12;
                case SslVersion.TlsV13:
                    return SslProtocols.Tls13;
                default:
                    throw new NotImplementedException(version.ToString());
            }
        }

        public ResultCode Handshake(string hostName)
        {
            StartSslOperation();
            _stream = new SslStream(new NetworkStream(((ManagedSocket)Socket).Socket, false), false, null, null);
            _stream.AuthenticateAsClient(hostName, null, TranslateSslVersion(_sslVersion), false);
            EndSslOperation();

            return ResultCode.Success;
        }

        public ResultCode Peek(out int peekCount, Memory<byte> buffer)
        {
            // NOTE: We cannot support that on .NET SSL API.
            // As Nintendo's curl implementation detail check if a connection is alive via Peek, we just return that it would block to let it know that it's alive.
            peekCount = -1;

            return ResultCode.WouldBlock;
        }

        public int Pending()
        {
            // Unsupported
            return 0;
        }

        public ResultCode Read(out int readCount, Memory<byte> buffer)
        {
            if (!Socket.Poll(0, SelectMode.SelectRead))
            {
                readCount = -1;

                return ResultCode.WouldBlock;
            }

            StartSslReadOperation();

            readCount = _stream.Read(buffer.Span);

            EndSslReadOperation();

            return ResultCode.Success;
        }

        public ResultCode Write(out int writtenCount, ReadOnlyMemory<byte> buffer)
        {
            if (!Socket.Poll(0, SelectMode.SelectWrite))
            {
                writtenCount = 0;

                return ResultCode.WouldBlock;
            }

            StartSslOperation();
            _stream.Write(buffer.Span);
            EndSslOperation();

            // .NET API doesn't provide the size written, assume all written.
            writtenCount = buffer.Length;

            return ResultCode.Success;
        }

        public ResultCode GetServerCertificate(string hostname, Span<byte> certificates, out uint storageSize, out uint certificateCount)
        {
            byte[] rawCertData = _stream.RemoteCertificate.GetRawCertData();

            rawCertData.CopyTo(certificates);

            storageSize = (uint)rawCertData.Length;
            certificateCount = 1;

            return ResultCode.Success;
        }

        public void Dispose()
        {
            _bsdContext.CloseFileDescriptor(SocketFd);
        }
    }
}
