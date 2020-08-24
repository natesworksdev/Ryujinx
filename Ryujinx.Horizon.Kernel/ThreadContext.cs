using System;
using System.Runtime.Intrinsics;

namespace Ryujinx.Horizon.Kernel
{
    class ThreadContext : IThreadContext
    {
        public ulong Frequency { get; }
        public ulong Counter => throw new NotImplementedException();

        public ulong TlsAddress { get; }

        public int Fpcr => 0;
        public int Fpsr => 0;
        public int Cpsr => 0;

        public bool Is32Bit { get; }

        public ThreadContext(ulong frequency, ulong tlsAddress, bool is32Bit)
        {
            Frequency = frequency;
            TlsAddress = tlsAddress;
            Is32Bit = is32Bit;
        }

        public ulong GetX(int index)
        {
            return default;
        }

        public void SetX(int index, ulong value)
        {
        }

        public Vector128<byte> GetV(int index)
        {
            return default;
        }

        public void SetV(int index, Vector128<byte> value)
        {
        }

        public void RequestInterrupt()
        {
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
        }
    }
}
