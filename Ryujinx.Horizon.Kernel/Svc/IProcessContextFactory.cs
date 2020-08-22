using Ryujinx.Cpu;
using Ryujinx.Memory;

namespace Ryujinx.Horizon.Kernel.Svc
{
    public interface IProcessContextFactory
    {
        IProcessContext Create(MemoryBlock backingMemory, ulong addressSpaceSize, InvalidAccessHandler invalidAccessHandler);
    }
}
