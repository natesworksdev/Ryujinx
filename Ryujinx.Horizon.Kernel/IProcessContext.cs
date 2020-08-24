using Ryujinx.Memory;
using System;

namespace Ryujinx.Horizon.Kernel
{
    public interface IProcessContext : IDisposable
    {
        IAddressSpaceManager AddressSpace { get; }

        IThreadContext CreateThreadContext(ulong timerFrequency, ulong tlsAddress, bool is32Bit);
        void Execute(IThreadContext context, ulong codeAddress);
    }
}
