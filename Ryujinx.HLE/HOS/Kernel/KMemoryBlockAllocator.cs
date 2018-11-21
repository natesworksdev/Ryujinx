namespace Ryujinx.HLE.HOS.Kernel
{
    class KMemoryBlockAllocator
    {
        private const int KMemoryBlockSize = 0x40;

        private ulong Size;

        public int Count { get; set; }

        public KMemoryBlockAllocator(ulong Size)
        {
            this.Size = Size;
        }

        public bool CanAllocate(int Count)
        {
            return (ulong)(this.Count + Count) * KMemoryBlockSize <= Size;
        }
    }
}