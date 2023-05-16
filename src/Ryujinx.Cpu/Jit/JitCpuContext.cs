using ARMeilleure.Memory;
using ARMeilleure.Translation;
using System.Threading.Tasks;

namespace Ryujinx.Cpu.Jit
{
    class JitCpuContext : ICpuContext
    {
        private readonly ITickSource _tickSource;
        private readonly Translator _translator;

        public JitCpuContext(ITickSource tickSource, IMemoryManager memory, bool for64Bit)
        {
            _tickSource = tickSource;
            _translator = new Translator(new JitMemoryAllocator(), memory, for64Bit);
            memory.UnmapEvent += UnmapHandler;
        }

        private void UnmapHandler(ulong address, ulong size)
        {
            _translator.InvalidateJitCacheRegion(address, size);
        }

        /// <inheritdoc/>
        public IExecutionContext CreateExecutionContext(ExceptionCallbacks exceptionCallbacks)
        {
            return new JitExecutionContext(new JitMemoryAllocator(), _tickSource, exceptionCallbacks);
        }

        /// <inheritdoc/>
        public async Task Execute(IExecutionContext context, ulong address)
        {
            _translator.Execute(((JitExecutionContext)context).Impl, address);
            await Task.Yield(); // TODO: look into how we could have jitted syscalls slot into 
        }

        /// <inheritdoc/>
        public void InvalidateCacheRegion(ulong address, ulong size)
        {
            _translator.InvalidateJitCacheRegion(address, size);
        }

        /// <inheritdoc/>
        public IDiskCacheLoadState LoadDiskCache(string titleIdText, string displayVersion, bool enabled)
        {
            return new JitDiskCacheLoadState(_translator.LoadDiskCache(titleIdText, displayVersion, enabled));
        }

        /// <inheritdoc/>
        public void PrepareCodeRange(ulong address, ulong size)
        {
            _translator.PrepareCodeRange(address, size);
        }
    }
}
