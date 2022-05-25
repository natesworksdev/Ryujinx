using ARMeilleure.Memory;

namespace Ryujinx.Cpu
{
    public interface ICpuEngine
    {
        ICpuContext CreateCpuContext(IMemoryManager memoryManager, bool for64Bit);
    }
}
