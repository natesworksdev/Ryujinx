using System;

namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd
{
    class BsdSocket : IBsdSocket
    {
        public int Family;
        public int Type;
        public int Protocol;
        public int Refcount { get; set; }

        public ISocket Handle;

        public bool Blocking => Handle.Blocking;

        public void Dispose()
        {
            Handle.Close();
            Handle.Dispose();
        }

        public LinuxError Read(out int readSize, Span<byte> buffer)
        {
            return Handle.Receive(out readSize, buffer, BsdSocketFlags.None);
        }

        public LinuxError Write(out int writeSize, ReadOnlySpan<byte> buffer)
        {
            return Handle.Send(out writeSize, buffer, BsdSocketFlags.None);
        }
    }
}