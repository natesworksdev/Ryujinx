using ARMeilleure.Memory;

namespace Ryujinx.Cpu
{
    public interface ICpuEngine
    {
        ITickSource TickSource { get; }
        ICpuContext CreateCpuContext(IMemoryManager memoryManager, bool for64Bit);
    }
}
