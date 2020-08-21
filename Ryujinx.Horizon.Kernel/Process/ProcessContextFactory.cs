using Ryujinx.Cpu;
using Ryujinx.Memory;

namespace Ryujinx.Horizon.Kernel.Process
{
    class ProcessContextFactory : IProcessContextFactory
    {
        public IProcessContext Create(MemoryBlock backingMemory, ulong addressSpaceSize, InvalidAccessHandler invalidAccessHandler)
        {
            return new ProcessContext(new AddressSpaceManager(backingMemory, addressSpaceSize));
        }
    }
}
