using Ryujinx.Graphics.Gal;
using System.IO;

namespace Ryujinx.ShaderTools
{
    internal class Memory : IGalMemory
    {
        private Stream _baseStream;

        private BinaryReader _reader;

        public Memory(Stream baseStream)
        {
            this._baseStream = baseStream;

            _reader = new BinaryReader(baseStream);
        }

        public int ReadInt32(long position)
        {
            _baseStream.Seek(position, SeekOrigin.Begin);

            return _reader.ReadInt32();
        }
    }
}
