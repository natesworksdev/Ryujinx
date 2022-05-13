using ARMeilleure.Memory;

namespace Ryujinx.Cpu.Jit
{
    public class JitEngine : ICpuEngine
    {
        private readonly JitTickSource _tickSource;
        public ITickSource TickSource => _tickSource;

        public JitEngine(ulong tickFrequency)
        {
            _tickSource = new JitTickSource(tickFrequency);
        }

        public ICpuContext CreateCpuContext(IMemoryManager memoryManager, bool for64Bit)
        {
            return new JitCpuContext(_tickSource, memoryManager, for64Bit);
        }
    }
}