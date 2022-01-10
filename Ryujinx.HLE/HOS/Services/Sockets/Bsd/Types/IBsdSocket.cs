using System;

namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd
{
    interface IBsdSocket : IDisposable
    {
        bool Blocking { get; }
        int Refcount { get; set; }

        LinuxError Read(out int readSize, Span<byte> buffer);

        LinuxError Write(out int writeSize, ReadOnlySpan<byte> buffer);
    }
}
