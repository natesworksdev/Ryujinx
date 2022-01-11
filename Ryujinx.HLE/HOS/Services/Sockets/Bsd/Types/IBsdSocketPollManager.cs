using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd
{
    interface IBsdSocketPollManager
    {
        bool IsCompatible(PollEvent evnt);

        LinuxError Poll(List<PollEvent> events, int timeoutMilliseconds, out int updatedCount);
    }
}
