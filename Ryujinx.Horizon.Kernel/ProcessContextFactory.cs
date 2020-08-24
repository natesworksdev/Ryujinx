using Ryujinx.Memory;

namespace Ryujinx.Horizon.Kernel
{
    class ProcessContextFactory : IProcessContextFactory
    {
        public IProcessContext Create(MemoryBlock backingMemory, ulong addressSpaceSize)
        {
            return new ProcessContext(new AddressSpaceManager(backingMemory, addressSpaceSize));
        }
    }
}
