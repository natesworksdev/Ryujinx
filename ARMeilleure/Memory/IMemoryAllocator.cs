namespace ARMeilleure.Memory
{
    public interface IMemoryAllocator
    {
        IMemoryBlock Allocate(ulong size);
    }
}
