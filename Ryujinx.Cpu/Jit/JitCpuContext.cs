using ARMeilleure.Memory;
using ARMeilleure.Translation;

namespace Ryujinx.Cpu.Jit
{
    class JitCpuContext : ICpuContext
    {
        private readonly JitTickSource _tickSource;
        private readonly Translator _translator;

        public JitCpuContext(JitTickSource tickSource, IMemoryManager memory, bool for64Bit)
        {
            _tickSource = tickSource;
            _translator = new Translator(new JitMemoryAllocator(), memory, for64Bit);
            memory.UnmapEvent += UnmapHandler;
        }

        private void UnmapHandler(ulong address, ulong size)
        {
            _translator.InvalidateJitCacheRegion(address, size);
        }

        public IExecutionContext CreateExecutionContext(ExceptionCallbacks exceptionCallbacks)
        {
            return new JitExecutionContext(new JitMemoryAllocator(), _tickSource, exceptionCallbacks);
        }

        public void Execute(IExecutionContext context, ulong address)
        {
            _translator.Execute(((JitExecutionContext)context).Impl, address);
        }

        public void InvalidateCacheRegion(ulong address, ulong size)
        {
            _translator.InvalidateJitCacheRegion(address, size);
        }
    }
}
