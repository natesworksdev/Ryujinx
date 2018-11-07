namespace Ryujinx.HLE.HOS.Kernel
{
    static class DramMemoryMap
    {
        public const long DramBase = 0x80000000;
        public const long DramSize = 0x100000000;
        public const long DramEnd  = DramBase + DramSize;

        public const long KernelReserveBase = DramBase + 0x60000;

        public const long SlabHeapBase = KernelReserveBase + 0x85000;
        public const long SlapHeapSize = 0xa21000;
    }
}