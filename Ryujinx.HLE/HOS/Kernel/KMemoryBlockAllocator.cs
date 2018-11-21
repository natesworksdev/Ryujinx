namespace Ryujinx.HLE.HOS.Kernel
{
    class KMemoryBlockAllocator
    {
        private ulong Size;

        public int Count { get; set; }

        public KMemoryBlockAllocator(ulong Size)
        {
            this.Size = Size;
        }

        public bool CanAllocate(int Count)
        {
            return (ulong)(this.Count + Count) * KMemoryManager.KMemoryBlockSize <= Size;
        }
    }
}