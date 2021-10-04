using Ryujinx.Graphics.Nvdec.Image;
using Ryujinx.Graphics.Nvdec.Types.Vp8;
using Ryujinx.Graphics.Nvdec.Vp8;
using Ryujinx.Graphics.Video;
using System;

namespace Ryujinx.Graphics.Nvdec
{
    static class Vp8Decoder
    {
        private static readonly Decoder _decoder = new Decoder();

        public static void Decode(NvdecDevice device, ResourceManager rm, ref NvdecRegisters state)
        {
            PictureInfo pictureInfo = rm.Gmm.DeviceRead<PictureInfo>(state.SetPictureInfoOffset);
            ReadOnlySpan<byte> bitstream = rm.Gmm.DeviceGetSpan(state.SetBitstreamOffset, (int)pictureInfo.VLDBufferSize);

            ISurface outputSurface = rm.Cache.Get(_decoder, CodecId.Vp8, 0, 0, pictureInfo.FrameWidth, pictureInfo.FrameHeight);

            Vp8PictureInfo info = pictureInfo.Convert();

            uint lumaOffset = state.SetSurfaceLumaOffset[3];
            uint chromaOffset = state.SetSurfaceChromaOffset[3];

            if (_decoder.Decode(ref info, outputSurface, bitstream))
            {
                SurfaceWriter.Write(rm.Gmm, outputSurface, lumaOffset, chromaOffset);

                device.OnFrameDecoded(CodecId.Vp8, lumaOffset, chromaOffset);
            }

            rm.Cache.Put(outputSurface);
        }
    }
}