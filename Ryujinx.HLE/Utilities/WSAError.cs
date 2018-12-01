namespace Ryujinx.HLE.Utilities
{
    internal enum WsaError
    {
        /*
        * All Windows Sockets error constants are biased by WSABASEERR from
        * the "normal"
        */
        BaseError                   = 10000,

        /*
        * Windows Sockets definitions of regular Microsoft C error constants
        */
        Interrupted                 = (BaseError + 4),
        BadFileHandle               = (BaseError + 9),
        AccessDenied                = (BaseError + 13),
        Fault                       = (BaseError + 14),
        InvalidArgument             = (BaseError + 22),
        TooManyOpenSockets          = (BaseError + 24),

        /*
         * Windows Sockets definitions of regular Berkeley error constants
         */
        WouldBlock                  = (BaseError + 35),
        InProgress                  = (BaseError + 36),
        AlreadyInProgress           = (BaseError + 37),
        NotSocket                   = (BaseError + 38),
        DestinationAddressRequired  = (BaseError + 39),
        MessageSize                 = (BaseError + 40),
        ProtocolType                = (BaseError + 41),
        ProtocolOption              = (BaseError + 42),
        ProtocolNotSupported        = (BaseError + 43),
        SocketNotSupported          = (BaseError + 44),
        OperationNotSupported       = (BaseError + 45),
        ProtocolFamilyNotSupported  = (BaseError + 46),
        AddressFamilyNotSupported   = (BaseError + 47),
        AddressAlreadyInUse         = (BaseError + 48),
        AddressNotAvailable         = (BaseError + 49),
        NetworkDown                 = (BaseError + 50),
        NetworkUnreachable          = (BaseError + 51),
        NetworkReset                = (BaseError + 52),
        ConnectionAborted           = (BaseError + 53),
        ConnectionReset             = (BaseError + 54),
        NoBufferSpaceAvailable      = (BaseError + 55),
        IsConnected                 = (BaseError + 56),
        NotConnected                = (BaseError + 57),
        Shutdown                    = (BaseError + 58),
        TooManyReferences           = (BaseError + 59),
        TimedOut                    = (BaseError + 60),
        ConnectionRefused           = (BaseError + 61),
        Loop                        = (BaseError + 62),
        NameTooLong                 = (BaseError + 63),
        HostDown                    = (BaseError + 64),
        HostUnreachable             = (BaseError + 65),
        NotEmpty                    = (BaseError + 66),
        ProcessLimit                = (BaseError + 67),
        UserQuota                   = (BaseError + 68),
        DiskQuota                   = (BaseError + 69),
        Stale                       = (BaseError + 70),
        Remote                      = (BaseError + 71),

        /*
         * Extended Windows Sockets error constant definitions
         */
        SystemNotReady              = (BaseError + 91),
        VersionNotSupported         = (BaseError + 92),
        NotInitialized              = (BaseError + 93),
        Disconnecting               = (BaseError + 101),
        NoMoreResultsOld            = (BaseError + 102),
        CancelledOld                = (BaseError + 103),
        InvalidProcedureCallTable   = (BaseError + 104),
        InvalidProvider             = (BaseError + 105),
        ProviderFailedInit          = (BaseError + 106),
        SysCallFailure              = (BaseError + 107),
        ServiceNotFound             = (BaseError + 108),
        TypeNotFound                = (BaseError + 109),
        NoMoreResults               = (BaseError + 110),
        Cancelled                   = (BaseError + 111),
        Refused                     = (BaseError + 112),

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
        HostNotFound           = (BaseError + 1001),

        /* Non-Authoritative: Host not found, or SERVERFAIL */
        TryAgain               = (BaseError + 1002),

        /* Non-recoverable errors, FORMERR, REFUSED, NOTIMP */
        NoRecovery             = (BaseError + 1003),

        /* Valid name, no data record of requested type */
        NoData                 = (BaseError + 1004),

        /*
         * Define QOS related error return codes
         *
         */
        QosReceivers           = (BaseError + 1005),
        /* at least one Reserve has arrived */
        QosSenders             = (BaseError + 1006),
        /* at least one Path has arrived */
        QosNoSenders           = (BaseError + 1007),
        /* there are no senders */
        QosNoReceivers         = (BaseError + 1008),
        /* there are no receivers */
        QosRequestConfirmed    = (BaseError + 1009),
        /* Reserve has been confirmed */
        QosAdmissionFailure    = (BaseError + 1010),
        /* error due to lack of resources */
        QosPolicyFailure       = (BaseError + 1011),
        /* rejected for administrative reasons - bad credentials */
        QosBadStyle            = (BaseError + 1012),
        /* unknown or conflicting style */
        QosBadObject           = (BaseError + 1013),
        /* problem with some part of the filterspec or providerspecific
         * buffer in general */
        QosTrafficCtrlError    = (BaseError + 1014),
        /* problem with some part of the flowspec */
        QosGenericError        = (BaseError + 1015),
    }
}
