using System;
using System.Runtime.Intrinsics;

namespace Ryujinx.Horizon.Kernel
{
    public interface IThreadContext : IDisposable
    {
        ulong Frequency { get; }
        ulong Counter { get; }

        ulong TlsAddress { get; }

        int Fpcr { get; }
        int Fpsr { get; }
        int Cpsr { get; }

        bool Is32Bit { get; }

        ulong GetX(int index);
        void SetX(int index, ulong value);

        Vector128<byte> GetV(int index);
        void SetV(int index, Vector128<byte> value);

        void RequestInterrupt();
        void Stop();
    }
}
