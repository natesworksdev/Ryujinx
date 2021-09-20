using Ryujinx.Graphics.Nvdec.H264;
using Ryujinx.Graphics.Nvdec.Image;
using Ryujinx.Graphics.Nvdec.Types.H264;
using Ryujinx.Graphics.Video;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Nvdec
{
    static class H264Decoder
    {
        private const int MbSizeInPixels = 16;

        public unsafe static void Decode(NvdecDecoderContext context, ResourceManager rm, ref NvdecRegisters state)
        {
            PictureInfo pictureInfo = rm.Gmm.DeviceRead<PictureInfo>(state.SetPictureInfoOffset);
            H264PictureInfo info = pictureInfo.Convert();

            ReadOnlySpan<byte> bitstream = rm.Gmm.DeviceGetSpan(state.SetBitstreamOffset, (int)pictureInfo.BitstreamSize);

            int width  = (int)pictureInfo.PicWidthInMbs * MbSizeInPixels;
            int height = (int)pictureInfo.PicHeightInMbs * MbSizeInPixels;

            int surfaceIndex = (int)pictureInfo.OutputSurfaceIndex;

            uint lumaOffset   = state.SetSurfaceLumaOffset[surfaceIndex];
            uint chromaOffset = state.SetSurfaceChromaOffset[surfaceIndex];

            uint frameNumber = state.SetFrameNumber;

            var frameOffsets = context.GetFrameOffsetsList();

            Decoder decoder = context.GetDecoder();

            ISurface outputSurface = rm.Cache.Get(decoder, CodecId.H264, 0, 0, width, height);

            bool hasTargetFrame = false;

            if (decoder.Decode(ref info, outputSurface, bitstream))
            {
                bool isCurrentFrame = outputSurface.FrameNumber == frameNumber;

                if (isCurrentFrame)
                {
                    SurfaceWriter.Write(rm.Gmm, outputSurface, lumaOffset, chromaOffset);
                    hasTargetFrame = true;
                }
                else if (TryFindOutOfOrderFrame(frameOffsets, (uint)outputSurface.FrameNumber, out var oooFrame))
                {
                    SurfaceWriter.Write(rm.Gmm, outputSurface, oooFrame.LumaOffset, oooFrame.ChromaOffset);
                }
            }

            if (!hasTargetFrame)
            {
                InsertOutOfOrderFrame(frameOffsets, frameNumber, lumaOffset, chromaOffset);
            }

            rm.Cache.Put(outputSurface);
        }

        private static void InsertOutOfOrderFrame(List<OutOfOrderFrame> list, uint frameNumber, uint lumaOffset, uint chromaOffset)
        {
            var item = new OutOfOrderFrame(false, frameNumber, lumaOffset, chromaOffset);

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].WasConsumed)
                {
                    list[i] = item;
                    return;
                }
            }

            list.Add(item);
        }

        private static bool TryFindOutOfOrderFrame(List<OutOfOrderFrame> list, uint frameNumber, out OutOfOrderFrame oooFrame)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (!item.WasConsumed && item.FrameNumber == frameNumber)
                {
                    list[i] = OutOfOrderFrame.Consumed;
                    oooFrame = item;
                    return true;
                }
            }

            oooFrame = default;
            return false;
        }
    }
}
