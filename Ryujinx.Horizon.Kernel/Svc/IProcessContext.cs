using ARMeilleure.State;
using Ryujinx.Memory;
using System;

namespace Ryujinx.Horizon.Kernel.Svc
{
    public interface IProcessContext : IDisposable
    {
        IAddressSpaceManager AddressSpace { get; }

        void Execute(ExecutionContext context, ulong codeAddress);
    }
}
