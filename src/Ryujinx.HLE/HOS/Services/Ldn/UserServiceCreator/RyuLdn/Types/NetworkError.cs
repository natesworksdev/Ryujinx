using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.RyuLdn.Types
{
    enum NetworkError : int
    {
        None,

        PortUnreachable,

        TooManyPlayers,
        VersionTooLow,
        VersionTooHigh,

        ConnectFailure,
        ConnectNotFound,
        ConnectTimeout,
        ConnectRejected,

        RejectFailed,

        Unknown = -1
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x4)]
    struct NetworkErrorMessage
    {
        public NetworkError Error;
    }
}