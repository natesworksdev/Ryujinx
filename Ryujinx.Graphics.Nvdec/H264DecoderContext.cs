using Ryujinx.Graphics.Nvdec.FFmpeg.H264;
using System;

namespace Ryujinx.Graphics.Nvdec
{
    class H264DecoderContext : IDisposable
    {
        private Decoder _decoder;

        public Decoder GetDecoder()
        {
            return _decoder ??= new Decoder();
        }

        public void Dispose()
        {
            _decoder?.Dispose();
            _decoder = null;
        }
    }
}