namespace Ryujinx.Memory.Tracking
{
    public class RegionHandle : RegionHandleBase
    {
        public override bool Dirty { get; protected set; }
        internal int SequenceNumber { get; set; }

        public RegionHandle(MemoryTracking tracking, ulong address, ulong size, bool mapped = true) : base(tracking, address, size, mapped)
        {
            Dirty = mapped;
        }
    }
}
