using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.HLE.Utilities
{
    public enum WsaError
    {
        /*
        * All Windows Sockets error constants are biased by WSABASEERR from
        * the "normal"
        */
        Wsabaseerr                 = 10000,
    
        /*
        * Windows Sockets definitions of regular Microsoft C error constants
        */
        Wsaeintr                   = (Wsabaseerr + 4),
        Wsaebadf                   = (Wsabaseerr + 9),
        Wsaeacces                  = (Wsabaseerr + 13),
        Wsaefault                  = (Wsabaseerr + 14),
        Wsaeinval                  = (Wsabaseerr + 22),
        Wsaemfile                  = (Wsabaseerr + 24),

        /*
         * Windows Sockets definitions of regular Berkeley error constants
         */
        Wsaewouldblock              = (Wsabaseerr + 35),
        Wsaeinprogress              = (Wsabaseerr + 36),
        Wsaealready                 = (Wsabaseerr + 37),
        Wsaenotsock                 = (Wsabaseerr + 38),
        Wsaedestaddrreq             = (Wsabaseerr + 39),
        Wsaemsgsize                 = (Wsabaseerr + 40),
        Wsaeprototype               = (Wsabaseerr + 41),
        Wsaenoprotoopt              = (Wsabaseerr + 42),
        Wsaeprotonosupport          = (Wsabaseerr + 43),
        Wsaesocktnosupport          = (Wsabaseerr + 44),
        Wsaeopnotsupp               = (Wsabaseerr + 45),
        Wsaepfnosupport             = (Wsabaseerr + 46),
        Wsaeafnosupport             = (Wsabaseerr + 47),
        Wsaeaddrinuse               = (Wsabaseerr + 48),
        Wsaeaddrnotavail            = (Wsabaseerr + 49),
        Wsaenetdown                 = (Wsabaseerr + 50),
        Wsaenetunreach              = (Wsabaseerr + 51),
        Wsaenetreset                = (Wsabaseerr + 52),
        Wsaeconnaborted             = (Wsabaseerr + 53),
        Wsaeconnreset               = (Wsabaseerr + 54),
        Wsaenobufs                  = (Wsabaseerr + 55),
        Wsaeisconn                  = (Wsabaseerr + 56),
        Wsaenotconn                 = (Wsabaseerr + 57),
        Wsaeshutdown                = (Wsabaseerr + 58),
        Wsaetoomanyrefs             = (Wsabaseerr + 59),
        Wsaetimedout                = (Wsabaseerr + 60),
        Wsaeconnrefused             = (Wsabaseerr + 61),
        Wsaeloop                    = (Wsabaseerr + 62),
        Wsaenametoolong             = (Wsabaseerr + 63),
        Wsaehostdown                = (Wsabaseerr + 64),
        Wsaehostunreach             = (Wsabaseerr + 65),
        Wsaenotempty                = (Wsabaseerr + 66),
        Wsaeproclim                 = (Wsabaseerr + 67),
        Wsaeusers                   = (Wsabaseerr + 68),
        Wsaedquot                   = (Wsabaseerr + 69),
        Wsaestale                   = (Wsabaseerr + 70),
        Wsaeremote                  = (Wsabaseerr + 71),

        /*
         * Extended Windows Sockets error constant definitions
         */
        Wsasysnotready             = (Wsabaseerr + 91),
        Wsavernotsupported         = (Wsabaseerr + 92),
        Wsanotinitialised          = (Wsabaseerr + 93),
        Wsaediscon                 = (Wsabaseerr + 101),
        Wsaenomore                 = (Wsabaseerr + 102),
        Wsaecancelled              = (Wsabaseerr + 103),
        Wsaeinvalidproctable       = (Wsabaseerr + 104),
        Wsaeinvalidprovider        = (Wsabaseerr + 105),
        Wsaeproviderfailedinit     = (Wsabaseerr + 106),
        Wsasyscallfailure          = (Wsabaseerr + 107),
        WsaserviceNotFound       = (Wsabaseerr + 108),
        WsatypeNotFound          = (Wsabaseerr + 109),
        WsaENoMore              = (Wsabaseerr + 110),
        WsaECancelled            = (Wsabaseerr + 111),
        Wsaerefused                = (Wsabaseerr + 112),

        /*
         * Error return codes from gethostbyname() and gethostbyaddr()
         * (when using the resolver). Note that these errors are
         * retrieved via WSAGetLastError() and must therefore follow
         * the rules for avoiding clashes with error numbers from
         * specific implementations or language run-time systems.
         * For this reason the codes are based at WSABASEERR+1001.
         * Note also that [WSA]NO_ADDRESS is defined only for
         * compatibility purposes.
         */

        /* Authoritative Answer: Host not found */
        WsahostNotFound          = (Wsabaseerr + 1001),

        /* Non-Authoritative: Host not found, or SERVERFAIL */
        WsatryAgain               = (Wsabaseerr + 1002),

        /* Non-recoverable errors, FORMERR, REFUSED, NOTIMP */
        WsanoRecovery             = (Wsabaseerr + 1003),

        /* Valid name, no data record of requested type */
        WsanoData                 = (Wsabaseerr + 1004),

        /*
         * Define QOS related error return codes
         *
         */
        WsaQosReceivers          = (Wsabaseerr + 1005),
        /* at least one Reserve has arrived */
        WsaQosSenders            = (Wsabaseerr + 1006),
        /* at least one Path has arrived */
        WsaQosNoSenders         = (Wsabaseerr + 1007),
        /* there are no senders */
        WsaQosNoReceivers       = (Wsabaseerr + 1008),
        /* there are no receivers */
        WsaQosRequestConfirmed  = (Wsabaseerr + 1009),
        /* Reserve has been confirmed */
        WsaQosAdmissionFailure  = (Wsabaseerr + 1010),
        /* error due to lack of resources */
        WsaQosPolicyFailure     = (Wsabaseerr + 1011),
        /* rejected for administrative reasons - bad credentials */
        WsaQosBadStyle          = (Wsabaseerr + 1012),
        /* unknown or conflicting style */
        WsaQosBadObject         = (Wsabaseerr + 1013),
        /* problem with some part of the filterspec or providerspecific
         * buffer in general */
        WsaQosTrafficCtrlError = (Wsabaseerr + 1014),
        /* problem with some part of the flowspec */
        WsaQosGenericError      = (Wsabaseerr + 1015),
    }
}
