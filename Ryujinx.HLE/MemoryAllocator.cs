using ARMeilleure.Memory;

namespace Ryujinx.HLE
{
    class MemoryAllocator : IMemoryAllocator
    {
        public IMemoryBlock Allocate(ulong size) => new MemoryBlockWrapper(size);
    }
}
