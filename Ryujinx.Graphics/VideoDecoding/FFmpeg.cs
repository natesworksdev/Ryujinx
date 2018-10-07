using FFmpeg.AutoGen;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.VideoDecoding
{
    unsafe static class FFmpegWrapper
    {
        private static AVCodec*        Codec;
        private static AVCodecContext* Context;
        private static AVFrame*        Frame;

        public static bool IsInitialized { get; private set; }

        public static void H264Initialize()
        {
            EnsureCodecInitialized(AVCodecID.AV_CODEC_ID_H264);
        }

        public static void Vp9Initialize()
        {
            EnsureCodecInitialized(AVCodecID.AV_CODEC_ID_VP9);
        }

        private static void EnsureCodecInitialized(AVCodecID CodecId)
        {
            if (IsInitialized)
            {
                Uninitialize();
            }

            Codec   = ffmpeg.avcodec_find_decoder(CodecId);
            Context = ffmpeg.avcodec_alloc_context3(Codec);
            Frame   = ffmpeg.av_frame_alloc();

            ffmpeg.avcodec_open2(Context, Codec, null);

            IsInitialized = true;
        }

        public static int DecodeFrame(byte[] Data)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Tried to use uninitialized codec!");
            }

            AVPacket Packet;

            ffmpeg.av_init_packet(&Packet);

            fixed (byte* Ptr = Data)
            {
                Packet.data = Ptr;
                Packet.size = Data.Length;

                ffmpeg.avcodec_send_packet(Context, &Packet);
            }

            return ffmpeg.avcodec_receive_frame(Context, Frame);
        }

        public static FFmpegFrame GetFrame()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Tried to use uninitialized codec!");
            }

            AVFrame ManagedFrame = Marshal.PtrToStructure<AVFrame>((IntPtr)Frame);

            byte*[] Data = ManagedFrame.data.ToArray();

            return new FFmpegFrame()
            {
                Width  = ManagedFrame.width,
                Height = ManagedFrame.height,

                LumaPtr    = Data[0],
                ChromaRPtr = Data[1],
                ChromaBPtr = Data[2]
            };
        }

        public static void Uninitialize()
        {
            if (IsInitialized)
            {
                ffmpeg.av_frame_unref(Frame);
                ffmpeg.av_free(Frame);
                ffmpeg.avcodec_close(Context);

                IsInitialized = false;
            }
        }
    }
}