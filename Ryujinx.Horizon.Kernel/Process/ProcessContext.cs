using ARMeilleure.State;
using Ryujinx.Memory;
using System;

namespace Ryujinx.Horizon.Kernel.Process
{
    class ProcessContext : IProcessContext
    {
        public IAddressSpaceManager AddressSpace { get; }

        public ProcessContext(IAddressSpaceManager asManager)
        {
            AddressSpace = asManager;
        }

        public void Execute(ExecutionContext context, ulong codeAddress)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }
    }
}
