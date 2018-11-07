namespace Ryujinx.HLE.HOS.Kernel
{
    struct KMemoryArrangeRegion
    {
        public long Address { get; private set; }
        public long Size    { get; private set; }

        public long EndAddr => Address + Size;

        public KMemoryArrangeRegion(long Address, long Size)
        {
            this.Address = Address;
            this.Size    = Size;
        }
    }
}