using FFmpeg.AutoGen;
using Ryujinx.Graphics.Video;
using System;

namespace Ryujinx.Graphics.Nvdec.FFmpeg.Vp8
{
    public sealed class Decoder : IDecoder
    {
        public bool IsHardwareAccelerated => false;

        private readonly FFmpegContext _context = new FFmpegContext(AVCodecID.AV_CODEC_ID_VP8);

        public ISurface CreateSurface(int width, int height)
        {
            return new Surface(width, height);
        }

        public bool Decode(ref Vp8PictureInfo pictureInfo, ISurface output, ReadOnlySpan<byte> bitstream)
        {
            Surface outSurf = (Surface)output;

            int uncompHeaderSize = pictureInfo.KeyFrame ? 10 : 3;

            byte[] header = new byte[uncompHeaderSize];

            uint firstPartSizeShifted = pictureInfo.FirstPartSize << 5;

            header[0] = (byte)(pictureInfo.KeyFrame ? 0 : 1);
            header[0] |= (byte)((pictureInfo.Version & 7) << 1);
            header[0] |= 1 << 4;
            header[0] |= (byte)firstPartSizeShifted;
            header[1] |= (byte)(firstPartSizeShifted >> 8);
            header[2] |= (byte)(firstPartSizeShifted >> 16);

            if (pictureInfo.KeyFrame)
            {
                header[3] = 0x9d;
                header[4] = 0x01;
                header[5] = 0x2a;
                header[6] = (byte)pictureInfo.FrameWidth;
                header[7] = (byte)((pictureInfo.FrameWidth >> 8) & 0x3F);
                header[8] = (byte)pictureInfo.FrameHeight;
                header[9] = (byte)((pictureInfo.FrameHeight >> 8) & 0x3F);
            }

            byte[] frame = new byte[bitstream.Length + uncompHeaderSize];

            header.CopyTo(frame, 0);
            bitstream.CopyTo(new Span<byte>(frame).Slice(uncompHeaderSize));

            return _context.DecodeFrame(outSurf, frame) == 0;
        }

        public void Dispose() => _context.Dispose();
    }
}