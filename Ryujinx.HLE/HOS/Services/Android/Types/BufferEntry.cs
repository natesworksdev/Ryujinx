namespace Ryujinx.HLE.HOS.Services.Android.Types
{
    struct BufferEntry
    {
        public BufferState State;

        public HalTransform Transform;

        public Rect Crop;

        public GbpBuffer Data;
    }
}