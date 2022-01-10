using System;

namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd
{
    struct PollEventData
    {
        [Flags]
        public enum EventTypeMask : ushort
        {
            Input        = 1,
            UrgentInput  = 2,
            Output       = 4,
            Error        = 8,
            Disconnected = 0x10,
            Invalid      = 0x20
        }

#pragma warning disable CS0649
        public int SocketFd;
        public EventTypeMask InputEvents;
#pragma warning restore CS0649
        public EventTypeMask OutputEvents;
    }
}
