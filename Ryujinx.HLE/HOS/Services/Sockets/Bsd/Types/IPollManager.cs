using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd
{
    interface IPollManager
    {
        bool IsCompatible(PollEvent evnt);

        LinuxError Poll(List<PollEvent> events, int timeoutMilliseconds, out int updatedCount);

        LinuxError Select(List<PollEvent> events, int timeout, out int updatedCount);
    }
}