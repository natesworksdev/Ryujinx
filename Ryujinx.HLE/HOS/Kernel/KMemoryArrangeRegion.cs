namespace Ryujinx.HLE.HOS.Kernel
{
    struct KMemoryArrangeRegion
    {
        public ulong Address { get; private set; }
        public ulong Size    { get; private set; }

        public ulong EndAddr => Address + Size;

        public KMemoryArrangeRegion(ulong address, ulong size)
        {
            this.Address = address;
            this.Size    = size;
        }
    }
}