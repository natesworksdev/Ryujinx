using System;
using System.IO;

namespace Ryujinx.HLE.HOS
{
    class HomebrewRomFs : Stream
    {
        private Stream _baseStream;
        private long   _basePosition;

        public HomebrewRomFs(Stream baseStream, long basePosition)
        {
            _baseStream   = baseStream;
            _basePosition = basePosition;

            _baseStream.Position = _basePosition;
        }

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => _baseStream.CanSeek;

        public override bool CanWrite => _baseStream.CanWrite;

        public override long Length => _baseStream.Length - _basePosition;

        public override long Position
        {
            get
            {
                return _baseStream.Position - _basePosition;
            }
            set
            {
                _baseStream.Position = value + _basePosition;
            }
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _baseStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                offset += _basePosition;
            }

           return _baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
