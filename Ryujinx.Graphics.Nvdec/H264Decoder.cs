using Ryujinx.Graphics.Nvdec.H264;
using Ryujinx.Graphics.Nvdec.Image;
using Ryujinx.Graphics.Nvdec.Types.H264;
using Ryujinx.Graphics.Video;
using System;

namespace Ryujinx.Graphics.Nvdec
{
    static class H264Decoder
    {
        private const int MbSizeInPixels = 16;

        public unsafe static void Decode(NvdecDevice device, NvdecDecoderContext context, ResourceManager rm, ref NvdecRegisters state)
        {
            PictureInfo pictureInfo = rm.Gmm.DeviceRead<PictureInfo>(state.SetPictureInfoOffset);
            H264PictureInfo info = pictureInfo.Convert();

            ReadOnlySpan<byte> bitstream = rm.Gmm.DeviceGetSpan(state.SetBitstreamOffset, (int)pictureInfo.BitstreamSize);

            int width  = (int)pictureInfo.PicWidthInMbs * MbSizeInPixels;
            int height = (int)pictureInfo.PicHeightInMbs * MbSizeInPixels;

            Decoder decoder = context.GetDecoder();

            ISurface outputSurface = rm.Cache.Get(decoder, CodecId.H264, 0, 0, width, height);

            if (decoder.Decode(ref info, outputSurface, bitstream))
            {
                int li = (int)pictureInfo.LumaOutputSurfaceIndex;
                int ci = (int)pictureInfo.ChromaOutputSurfaceIndex;

                uint lumaOffset   = state.SetSurfaceLumaOffset[li];
                uint chromaOffset = state.SetSurfaceChromaOffset[ci];

                SurfaceWriter.Write(rm.Gmm, outputSurface, lumaOffset, chromaOffset);

                device.OnFrameDecoded(CodecId.H264, lumaOffset, chromaOffset);
            }

            rm.Cache.Put(outputSurface);
        }
    }
}
