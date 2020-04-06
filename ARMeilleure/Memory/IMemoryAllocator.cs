namespace ARMeilleure.Memory
{
    public interface IMemoryAllocator
    {
        IMemoryBlock Allocate(ulong size);
        IMemoryBlock Reserve(ulong size);
    }
}
