namespace Ryujinx.Horizon.Kernel.Memory
{
    enum KMemoryOperation
    {
        MapPa,
        MapVa,
        Allocate,
        Unmap,
        ChangePermRw,
        ChangePermsAndAttributes
    }
}