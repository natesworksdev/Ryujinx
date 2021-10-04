using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Graphics.Nvdec.Image;
using Ryujinx.Graphics.Nvdec.Types.Vp8;
using Ryujinx.Graphics.Video;
using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Vpx.Net;

namespace Ryujinx.Graphics.Nvdec
{
    static class Vp8Decoder
    {
        //private const int MbSizeInPixels = 16;

        //private static readonly Decoder _decoder = new Decoder();

        private static readonly VP8Codec _vp8Codec = new VP8Codec();
        private static vpx_codec_ctx_t _vp8Decoder;
        private static object _decoderLock = new object();

        public unsafe static void Decode(NvdecDevice device, ResourceManager rm, ref NvdecRegisters state)
        {
            PictureInfo pictureInfo = rm.Gmm.DeviceRead<PictureInfo>(state.SetPictureInfoOffset);
            ReadOnlySpan<byte> bitStream = rm.Gmm.DeviceGetSpan(state.SetBitstreamOffset, (int)pictureInfo.HistBufferSize);

            //File.WriteAllBytes("nvdec.vp8.bin", bitStream.ToArray());

            /*
            var decodedFrame = _vp8Codec.DecodeVideo(bitStream.ToArray(), VideoPixelFormatsEnum.Rgb, VideoCodecsEnum.VP8);

            var rgbSample = decodedFrame.First();
            */

            lock (_decoderLock)
            {
                if (_vp8Decoder == null)
                {
                    _vp8Decoder = new vpx_codec_ctx_t();
                    vpx_codec_iface_t algo = vp8_dx.vpx_codec_vp8_dx();
                    vpx_codec_dec_cfg_t cfg = new vpx_codec_dec_cfg_t { threads = 1 };
                    vpx_codec_err_t res = vpx_decoder.vpx_codec_dec_init(_vp8Decoder, algo, cfg, 0);
                }

                Logger.Error?.PrintMsg(LogClass.Nvdec, $"Attempting to decode {bitStream.ToArray().Length} bytes.");
                Console.WriteLine(HexUtils.HexTable(bitStream.ToArray()));

                byte[] frame = bitStream.ToArray();

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
                    int dwidth = (int)img.d_w;
                    int dheight = (int)img.d_h;
                    int sz = dwidth * dheight;

                    var yPlane = img.planes[0];
                    var uPlane = img.planes[1];
                    var vPlane = img.planes[2];

                    byte[] decodedBuffer = new byte[dwidth * dheight * 3 / 2];

                    for (uint row = 0; row < dheight; row++)
                    {
                        Marshal.Copy((IntPtr)(yPlane + row * img.stride[0]), decodedBuffer, (int)(row * dwidth), (int)dwidth);

                        if (row < dheight / 2)
                        {
                            Marshal.Copy((IntPtr)(uPlane + row * img.stride[1]), decodedBuffer, (int)(sz + row * (dwidth / 2)), (int)dwidth / 2);
                            Marshal.Copy((IntPtr)(vPlane + row * img.stride[2]), decodedBuffer, (int)(sz + sz / 4 + row * (dwidth / 2)), (int)dwidth / 2);
                        }
                    }

                    byte[] rgb = PixelConverter.I420toBGR(decodedBuffer, dwidth, dheight, out _);

                    fixed (byte* bmpPtr = rgb)
                    {
                        Bitmap bmp = new Bitmap(dwidth, dheight, dwidth * 3, System.Drawing.Imaging.PixelFormat.Format24bppRgb, new IntPtr(bmpPtr));
                        bmp.Save("decodekeyframe.bmp");
                        bmp.Dispose();
                    }
                    //return new List<VideoSample> { new VideoSample { Width = img.d_w, Height = img.d_h, Sample = rgb } };
                }

                //return new List<VideoSample>();
            }

            /*
            ISurface outputSurface = rm.Cache.Get(_decoder, CodecId.Vp8, 0, 0, pictureInfo.FrameWidth, pictureInfo.FrameHeight);

            rm.Cache.Put(outputSurface);

            /*
        PictureInfo pictureInfo = rm.Gmm.DeviceRead<PictureInfo>(state.SetPictureInfoOffset);
        H264PictureInfo info = pictureInfo.Convert();

        ReadOnlySpan<byte> bitstream = rm.Gmm.DeviceGetSpan(state.SetBitstreamOffset, (int)pictureInfo.BitstreamSize);

        int width  = (int)pictureInfo.PicWidthInMbs * MbSizeInPixels;
        int height = (int)pictureInfo.PicHeightInMbs * MbSizeInPixels;

        ISurface outputSurface = rm.Cache.Get(_decoder, CodecId.H264, 0, 0, width, height);

        if (_decoder.Decode(ref info, outputSurface, bitstream))
        {
            int li = (int)pictureInfo.LumaOutputSurfaceIndex;
            int ci = (int)pictureInfo.ChromaOutputSurfaceIndex;

            uint lumaOffset   = state.SetSurfaceLumaOffset[li];
            uint chromaOffset = state.SetSurfaceChromaOffset[ci];

            SurfaceWriter.Write(rm.Gmm, outputSurface, lumaOffset, chromaOffset);

            device.OnFrameDecoded(CodecId.H264, lumaOffset, chromaOffset);
        }

        rm.Cache.Put(outputSurface);
            */
        }
    }
}
