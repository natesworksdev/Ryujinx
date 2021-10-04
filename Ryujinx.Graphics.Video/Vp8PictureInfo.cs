using Ryujinx.Common.Memory;

namespace Ryujinx.Graphics.Video
{
    public ref struct Vp8PictureInfo
    {
        public uint KeyFrame;
        public uint FirstPartSize;
        public uint Version;
        public ushort FrameWidth;
        public ushort FrameHeight;
    }
}
