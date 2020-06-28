using System;

namespace Ryujinx.Graphics.Video
{
    public interface IH264Decoder : IDecoder
    {
        void Decode(ref H264PictureInfo pictureInfo, ReadOnlySpan<byte> data);
    }
}
