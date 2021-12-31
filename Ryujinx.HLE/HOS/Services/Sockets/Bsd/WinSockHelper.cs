using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd
{
    static class WinSockHelper
    {
        private static readonly Dictionary<WsaError, LinuxError> _errorMap = new()
        {
            // WSAEINTR
            {WsaError.WSAEINTR,           LinuxError.EINTR},
            // WSAEWOULDBLOCK
            {WsaError.WSAEWOULDBLOCK,     LinuxError.EWOULDBLOCK},
            // WSAEINPROGRESS
            {WsaError.WSAEINPROGRESS,     LinuxError.EINPROGRESS},
            // WSAEALREADY
            {WsaError.WSAEALREADY,        LinuxError.EALREADY},
            // WSAENOTSOCK
            {WsaError.WSAENOTSOCK,        LinuxError.ENOTSOCK},
            // WSAEDESTADDRREQ
            {WsaError.WSAEDESTADDRREQ,    LinuxError.EDESTADDRREQ},
            // WSAEMSGSIZE
            {WsaError.WSAEMSGSIZE,        LinuxError.EMSGSIZE},
            // WSAEPROTOTYPE
            {WsaError.WSAEPROTOTYPE,      LinuxError.EPROTOTYPE},
            // WSAENOPROTOOPT
            {WsaError.WSAENOPROTOOPT,     LinuxError.ENOPROTOOPT},
            // WSAEPROTONOSUPPORT
            {WsaError.WSAEPROTONOSUPPORT, LinuxError.EPROTONOSUPPORT},
            // WSAESOCKTNOSUPPORT
            {WsaError.WSAESOCKTNOSUPPORT, LinuxError.ESOCKTNOSUPPORT},
            // WSAEOPNOTSUPP
            {WsaError.WSAEOPNOTSUPP,      LinuxError.EOPNOTSUPP},
            // WSAEPFNOSUPPORT
            {WsaError.WSAEPFNOSUPPORT,    LinuxError.EPFNOSUPPORT},
            // WSAEAFNOSUPPORT
            {WsaError.WSAEAFNOSUPPORT,    LinuxError.EAFNOSUPPORT},
            // WSAEADDRINUSE
            {WsaError.WSAEADDRINUSE,      LinuxError.EADDRINUSE},
            // WSAEADDRNOTAVAIL
            {WsaError.WSAEADDRNOTAVAIL,   LinuxError.EADDRNOTAVAIL},
            // WSAENETDOWN
            {WsaError.WSAENETDOWN,        LinuxError.ENETDOWN},
            // WSAENETUNREACH
            {WsaError.WSAENETUNREACH,     LinuxError.ENETUNREACH},
            // WSAENETRESET
            {WsaError.WSAENETRESET,       LinuxError.ENETRESET},
            // WSAECONNABORTED
            {WsaError.WSAECONNABORTED,    LinuxError.ECONNABORTED},
            // WSAECONNRESET
            {WsaError.WSAECONNRESET,      LinuxError.ECONNRESET},
            // WSAENOBUFS
            {WsaError.WSAENOBUFS,         LinuxError.ENOBUFS},
            // WSAEISCONN
            {WsaError.WSAEISCONN,         LinuxError.EISCONN},
            // WSAENOTCONN
            {WsaError.WSAENOTCONN,        LinuxError.ENOTCONN},
            // WSAESHUTDOWN
            {WsaError.WSAESHUTDOWN,       LinuxError.ESHUTDOWN},
            // WSAETOOMANYREFS
            {WsaError.WSAETOOMANYREFS,    LinuxError.ETOOMANYREFS},
            // WSAETIMEDOUT
            {WsaError.WSAETIMEDOUT,       LinuxError.ETIMEDOUT},
            // WSAECONNREFUSED
            {WsaError.WSAECONNREFUSED,    LinuxError.ECONNREFUSED},
            // WSAELOOP
            {WsaError.WSAELOOP,           LinuxError.ELOOP},
            // WSAENAMETOOLONG
            {WsaError.WSAENAMETOOLONG,    LinuxError.ENAMETOOLONG},
            // WSAEHOSTDOWN
            {WsaError.WSAEHOSTDOWN,       LinuxError.EHOSTDOWN},
            // WSAEHOSTUNREACH
            {WsaError.WSAEHOSTUNREACH,    LinuxError.EHOSTUNREACH},
            // WSAENOTEMPTY
            {WsaError.WSAENOTEMPTY,       LinuxError.ENOTEMPTY},
            // WSAEUSERS
            {WsaError.WSAEUSERS,          LinuxError.EUSERS},
            // WSAEDQUOT
            {WsaError.WSAEDQUOT,          LinuxError.EDQUOT},
            // WSAESTALE
            {WsaError.WSAESTALE,          LinuxError.ESTALE},
            // WSAEREMOTE
            {WsaError.WSAEREMOTE,         LinuxError.EREMOTE},
            // WSAEINVAL
            {WsaError.WSAEINVAL,          LinuxError.EINVAL},
            // WSAEFAULT
            {WsaError.WSAEFAULT,          LinuxError.EFAULT},
            // NOERROR
            {0, 0}
        };

        public static LinuxError ConvertError(WsaError errorCode)
        {
            if (!_errorMap.TryGetValue(errorCode, out LinuxError errno))
            {
                errno = (LinuxError)errorCode;
            }

            return errno;
        }
    }
}
