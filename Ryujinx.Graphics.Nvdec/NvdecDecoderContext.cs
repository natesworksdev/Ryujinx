using Ryujinx.Graphics.Nvdec.H264;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Nvdec
{
    struct OutOfOrderFrame
    {
        public static OutOfOrderFrame Consumed => new OutOfOrderFrame(true, 0, 0, 0);

        public readonly bool WasConsumed;
        public readonly uint FrameNumber;
        public readonly uint LumaOffset;
        public readonly uint ChromaOffset;

        public OutOfOrderFrame(bool consumed, uint frameNumber, uint lumaOffset, uint chromaOffset)
        {
            WasConsumed = consumed;
            FrameNumber = frameNumber;
            LumaOffset = lumaOffset;
            ChromaOffset = chromaOffset;
        }
    }

    class NvdecDecoderContext : IDisposable
    {
        private Decoder _decoder;
        private List<OutOfOrderFrame> _frameOffsets;

        public Decoder GetDecoder()
        {
            return _decoder ??= new Decoder();
        }

        public List<OutOfOrderFrame> GetFrameOffsetsList()
        {
            return _frameOffsets ??= new List<OutOfOrderFrame>();
        }

        public void Dispose()
        {
            _decoder?.Dispose();
            _decoder = null;
            _frameOffsets = null;
        }
    }
}