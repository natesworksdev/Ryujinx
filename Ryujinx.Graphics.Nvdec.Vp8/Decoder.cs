using Ryujinx.Common.Logging;
using Ryujinx.Graphics.Video;
using SIPSorceryMedia.Abstractions;
using System;
using System.Runtime.InteropServices;
using Vpx.Net;

namespace Ryujinx.Graphics.Nvdec.Vp8
{
    public sealed class Decoder : IDecoder
    {
        public bool IsHardwareAccelerated => false;

        private static readonly VP8Codec _vp8Codec = new VP8Codec();
        private static vpx_codec_ctx_t _vp8Decoder;
        private static object _decoderLock = new object();

        public ISurface CreateSurface(int width, int height)
        {
            return new Surface(width, height);
        }

        public unsafe bool Decode(ref Vp8PictureInfo pictureInfo, ISurface output, ReadOnlySpan<byte> bitstream)
        {
            Surface outSurf = (Surface)output;

            lock (_decoderLock)
            {
                if (_vp8Decoder == null)
                {
                    _vp8Decoder = new vpx_codec_ctx_t();
                    vpx_codec_iface_t algo = vp8_dx.vpx_codec_vp8_dx();
                    vpx_codec_dec_cfg_t cfg = new vpx_codec_dec_cfg_t { threads = 1 };
                    vpx_codec_err_t res = vpx_decoder.vpx_codec_dec_init(_vp8Decoder, algo, cfg, 0);
                }

                //Logger.Error?.PrintMsg(LogClass.Nvdec, $"Attempting to decode {bitstream.ToArray().Length} bytes.");

                int uncompHeaderSize = pictureInfo.KeyFrame != 0 ? 10 : 3;

                byte[] header = new byte[uncompHeaderSize];

                uint firstPartSizeShifted = pictureInfo.FirstPartSize << 5;

                header[0] = (byte)(pictureInfo.KeyFrame != 0 ? 0 : 1);
                header[0] |= (byte)((pictureInfo.Version & 7) << 1);
                header[0] |= 1 << 4;
                header[0] |= (byte)firstPartSizeShifted;
                header[1] |= (byte)(firstPartSizeShifted >> 8);
                header[2] |= (byte)(firstPartSizeShifted >> 16);

                if (pictureInfo.KeyFrame != 0)
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

                fixed (byte* pFrame = frame)
                {
                    var result = vpx_decoder.vpx_codec_decode(_vp8Decoder, pFrame, (uint)frame.Length, IntPtr.Zero, 0);

                    if (result != vpx_codec_err_t.VPX_CODEC_OK)
                    {
                        Logger.Error?.PrintMsg(LogClass.Nvdec, $"VP8 decode of video sample failed with {result}.");
                    }
                }

                IntPtr iter = IntPtr.Zero;
                var img = vpx_decoder.vpx_codec_get_frame(_vp8Decoder, iter);

                if (img == null)
                {
                    Logger.Error?.PrintMsg(LogClass.Nvdec, "Image could not be acquired from VP8 decoder stage.");
                }
                else
                {
                    outSurf.YPlane = new Plane((IntPtr)img.planes[0], (int)img.d_w * img.stride[0]);
                    outSurf.UPlane = new Plane((IntPtr)img.planes[1], (int)img.d_w * img.stride[1]);
                    outSurf.VPlane = new Plane((IntPtr)img.planes[2], (int)img.d_w * img.stride[2]);

                    outSurf.Width = (int)img.d_w;
                    outSurf.Height = (int)img.d_h;

                    outSurf.Stride = img.stride[0];
                    outSurf.UvWidth = (int)img.d_w / 2;
                    outSurf.UvHeight = (int)img.d_h / 2;
                    outSurf.UvStride = img.stride[1];

                    return true;
                }

                return false;
            }
        }

        public void Dispose() { }
    }
}