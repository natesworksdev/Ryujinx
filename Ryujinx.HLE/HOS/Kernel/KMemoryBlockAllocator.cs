namespace Ryujinx.HLE.HOS.Kernel
{
    class KMemoryBlockAllocator
    {
        private ulong _capacityElements;

        public int Count { get; set; }

        public KMemoryBlockAllocator(ulong capacityElements)
        {
            this._capacityElements = capacityElements;
        }

        public bool CanAllocate(int count)
        {
            return (ulong)(this.Count + count) <= _capacityElements;
        }
    }
}