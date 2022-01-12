using System;

namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd
{
    [Flags]
    enum BsdSocketCreationFlags
    {
        None,
        CloseOnExecution,
        NonBlocking,


        FlagsShift = 28
    }
}
