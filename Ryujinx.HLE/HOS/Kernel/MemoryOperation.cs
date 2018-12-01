namespace Ryujinx.HLE.HOS.Kernel
{
    internal enum MemoryOperation
    {
        MapPa,
        MapVa,
        Allocate,
        Unmap,
        ChangePermRw,
        ChangePermsAndAttributes
    }
}