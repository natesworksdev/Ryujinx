namespace Ryujinx.Horizon.Kernel.Memory
{
    enum MemoryOperation
    {
        MapPa,
        MapVa,
        Allocate,
        Unmap,
        ChangePermRw,
        ChangePermsAndAttributes
    }
}