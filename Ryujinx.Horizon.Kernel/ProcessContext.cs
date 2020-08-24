using Ryujinx.Memory;
using System;

namespace Ryujinx.Horizon.Kernel
{
    class ProcessContext : IProcessContext
    {
        public IAddressSpaceManager AddressSpace { get; }

        public ProcessContext(IAddressSpaceManager asManager)
        {
            AddressSpace = asManager;
        }

        public IThreadContext CreateThreadContext(ulong timerFrequency, ulong tlsAddress, bool is32Bit)
        {
            return new ThreadContext(timerFrequency, tlsAddress, is32Bit);
        }

        public void Execute(IThreadContext context, ulong codeAddress)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }        
    }
}
