namespace Ryujinx.HLE.HOS.Kernel.Memory
{
    readonly struct KMemoryArrangeRegion
    {
        public ulong Address { get; }
        public ulong Size    { get; }

        public ulong EndAddr => Address + Size;

        public KMemoryArrangeRegion(ulong address, ulong size)
        {
            Address = address;
            Size    = size;
        }
    }
}