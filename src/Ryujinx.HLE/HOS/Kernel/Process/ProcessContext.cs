using Ryujinx.Cpu;
using Ryujinx.Memory;
using System;
using System.Threading.Tasks;

namespace Ryujinx.HLE.HOS.Kernel.Process
{
    class ProcessContext : IProcessContext
    {
        public IVirtualMemoryManager AddressSpace { get; }

        public ProcessContext(IVirtualMemoryManager asManager)
        {
            AddressSpace = asManager;
        }

        public IExecutionContext CreateExecutionContext(ExceptionCallbacks exceptionCallbacks)
        {
            return new ProcessExecutionContext();
        }

        public Task Execute(IExecutionContext context, ulong codeAddress)
        {
            throw new NotSupportedException();
        }

        public void InvalidateCacheRegion(ulong address, ulong size)
        {
        }

        public void Dispose()
        {
        }
    }
}
