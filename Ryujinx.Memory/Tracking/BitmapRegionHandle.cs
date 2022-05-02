namespace Ryujinx.Memory.Tracking
{
    public class BitmapRegionHandle : RegionHandleBase
    {
        internal MultithreadedBitmap Bitmap;
        internal int DirtyBit;

        public override bool Dirty
        {
            get
            {
                return Bitmap.IsSet(DirtyBit);
            }
            protected set
            {
                Bitmap.Set(DirtyBit, value);
            }
        }

        internal BitmapRegionHandle(MemoryTracking tracking, ulong address, ulong size, MultithreadedBitmap bitmap, int bit, bool mapped = true) : base(tracking, address, size, mapped)
        {
            Bitmap = bitmap;
            DirtyBit = bit;

            Dirty = mapped;
        }

        internal void ReplaceBitmap(MultithreadedBitmap bitmap, int bit)
        {
            // TODO: thread safe

            var oldBitmap = Bitmap;
            var oldBit = DirtyBit;

            bitmap.Set(bit, Dirty);

            Bitmap = bitmap;
            DirtyBit = bit;

            Dirty |= oldBitmap.IsSet(oldBit);
        }
    }
}
