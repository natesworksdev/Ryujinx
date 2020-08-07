using ARMeilleure.State;
using Ryujinx.Memory;
using System;

namespace Ryujinx.HLE.HOS.Kernel.Process
{
    interface IProcessContext : IDisposable
    {
        IAddressSpaceManager AddressSpace { get; }

        void Execute(ExecutionContext context, ulong codeAddress);
    }
}
