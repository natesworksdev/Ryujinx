namespace Ryujinx.Core.OsHle
{
    static class MemoryRegions
    {
        public const long RamSize        = 1L << 32;
        public const long AddrSpaceStart = 0x08000000;
        public const int  AddrSpaceBits  = 38;
        public const long AddrSpaceSize  = 1L << AddrSpaceBits;

        public const long MapRegionAddress = 0x10000000;
        public const long MapRegionSize    = 0x20000000;

        public const long MainStackSize = 0x100000;

        public const long MainStackAddress = RamSize - MainStackSize;

        public const long TlsPagesSize = 0x4000;

        public const long TlsPagesAddress = MainStackAddress - TlsPagesSize;

        public const long HeapRegionAddress = MapRegionAddress + MapRegionSize;

        public const long TotalMemoryUsed = HeapRegionAddress + TlsPagesSize + MainStackSize;

        public const long TotalMemoryAvailable = RamSize - AddrSpaceStart;

        
    }
}