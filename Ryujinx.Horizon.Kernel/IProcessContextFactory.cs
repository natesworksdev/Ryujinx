using Ryujinx.Memory;

namespace Ryujinx.Horizon.Kernel
{
    public interface IProcessContextFactory
    {
        IProcessContext Create(MemoryBlock backingMemory, ulong addressSpaceSize);
    }
}
