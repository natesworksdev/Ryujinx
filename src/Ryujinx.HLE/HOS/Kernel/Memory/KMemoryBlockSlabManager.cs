namespace Ryujinx.HLE.HOS.Kernel.Memory
{
    sealed class KMemoryBlockSlabManager
    {
        private ulong _capacityElements;

        public int Count { get; set; }

        public KMemoryBlockSlabManager(ulong capacityElements)
        {
            _capacityElements = capacityElements;
        }

        public bool CanAllocate(int count)
        {
            return (ulong)(Count + count) <= _capacityElements;
        }
    }
}