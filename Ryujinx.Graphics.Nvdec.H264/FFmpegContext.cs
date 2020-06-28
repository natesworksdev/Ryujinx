using FFmpeg.AutoGen;
using System;

namespace Ryujinx.Graphics.Nvdec.H264
{
    unsafe class FFmpegContext : IDisposable
    {
        private readonly AVCodec* _codec;
        private AVCodecContext* _context;

        public FFmpegContext()
        {
            _codec = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
            _context = ffmpeg.avcodec_alloc_context3(_codec);

            ffmpeg.avcodec_open2(_context, _codec, null);
        }

        public int DecodeFrame(ReadOnlySpan<byte> data)
        {
            AVPacket packet;

            ffmpeg.av_init_packet(&packet);

            fixed (byte* ptr = data)
            {
                packet.data = ptr;
                packet.size = data.Length;

                return ffmpeg.avcodec_send_packet(_context, &packet);
            }
        }

        public int ReceiveFrame(Surface surface)
        {
            return ffmpeg.avcodec_receive_frame(_context, surface.Frame);
        }

        public void Dispose()
        {
            ffmpeg.avcodec_close(_context);

            fixed (AVCodecContext** ppContext = &_context)
            {
                ffmpeg.avcodec_free_context(ppContext);
            }
        }
    }
}
