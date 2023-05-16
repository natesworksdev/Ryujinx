using ARMeilleure.Memory;
using System;
using System.Threading.Tasks;

namespace Ryujinx.Cpu.AppleHv
{
    class HvCpuContext : ICpuContext
    {
        private readonly ITickSource _tickSource;
        private readonly HvMemoryManager _memoryManager;

        public HvCpuContext(ITickSource tickSource, IMemoryManager memory, bool for64Bit)
        {
            _tickSource = tickSource;
            _memoryManager = (HvMemoryManager)memory;
        }

        private void UnmapHandler(ulong address, ulong size)
        {
        }

        /// <inheritdoc/>
        public IExecutionContext CreateExecutionContext(ExceptionCallbacks exceptionCallbacks)
        {
            return new HvExecutionContext(_tickSource, exceptionCallbacks);
        }

        /// <inheritdoc/>
        public async Task Execute(IExecutionContext context, ulong address)
        {
            await ((HvExecutionContext)context).Execute(_memoryManager, address);
        }

        /// <inheritdoc/>
        public void InvalidateCacheRegion(ulong address, ulong size)
        {
        }

        public IDiskCacheLoadState LoadDiskCache(string titleIdText, string displayVersion, bool enabled)
        {
            return new DummyDiskCacheLoadState();
        }

        public void PrepareCodeRange(ulong address, ulong size)
        {
        }
    }
}