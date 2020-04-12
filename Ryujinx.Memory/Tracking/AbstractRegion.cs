using Ryujinx.Memory.Range;

namespace Ryujinx.Memory.Tracking
{
    abstract class AbstractRegion : INonOverlappingRange
    {
        public ulong Address { get; }
        public ulong Size { get; protected set; }
        public ulong EndAddress => Address + Size;

        protected AbstractRegion(ulong address, ulong size)
        {
            Address = address;
            Size = size;
        }

        public bool OverlapsWith(ulong address, ulong size)
        {
            return Address < address + size && address < EndAddress;
        }

        /// <summary>
        /// Signals to the handles that a memory event has occurred, and unprotects the region. Assumes that the tracking lock has been obtained.
        /// </summary>
        /// <param name="write">Whether the region was written to or read</param>
        public abstract void Signal(ulong address, ulong size, bool write);

        public abstract INonOverlappingRange Split(ulong splitAddress);
    }
}
