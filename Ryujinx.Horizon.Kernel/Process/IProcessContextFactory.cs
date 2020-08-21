using Ryujinx.Cpu;
using Ryujinx.Memory;

namespace Ryujinx.Horizon.Kernel.Process
{
    public interface IProcessContextFactory
    {
        IProcessContext Create(MemoryBlock backingMemory, ulong addressSpaceSize, InvalidAccessHandler invalidAccessHandler);
    }
}
