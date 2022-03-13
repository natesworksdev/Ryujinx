using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Gpu.Shader
{
    struct BinarySerialization
    {
        private readonly Stream _stream;
        private Stream _activeStream;

        public BinarySerialization(Stream stream)
        {
            _stream = stream;
            _activeStream = stream;
        }

        public bool TryRead<T>(ref T data) where T : unmanaged
        {
            // Length is unknown on compressed streams.
            if (_activeStream == _stream)
            {
                int size = Unsafe.SizeOf<T>();
                if (_activeStream.Length - _activeStream.Position < size)
                {
                    return false;
                }
            }

            Span<byte> buffer = MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateSpan(ref data, 1));
            for (int offset = 0; offset < buffer.Length;)
            {
                offset += _activeStream.Read(buffer.Slice(offset));
            }

            return true;
        }

        public void ReadWithMagicAndSize<T>(ref T data, uint magic) where T : unmanaged
        {
            uint actualMagic = 0;
            int size = 0;
            TryRead(ref actualMagic);
            TryRead(ref size);
            // TODO: Throw if actualMagic != magic.
            // TODO: Throw if size > Unsafe.SizeOf<T>().
            Span<byte> buffer = MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateSpan(ref data, 1)).Slice(0, size);
            for (int offset = 0; offset < buffer.Length;)
            {
                offset += _activeStream.Read(buffer.Slice(offset));
            }
        }

        public void Write<T>(ref T data) where T : unmanaged
        {
            Span<byte> buffer = MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateSpan(ref data, 1));
            _activeStream.Write(buffer);
        }

        public void WriteWithMagicAndSize<T>(ref T data, uint magic) where T : unmanaged
        {
            int size = Unsafe.SizeOf<T>();
            Write(ref magic);
            Write(ref size);
            Span<byte> buffer = MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateSpan(ref data, 1));
            _activeStream.Write(buffer);
        }

        public void BeginCompression()
        {
            CompressionAlgorithm algorithm = CompressionAlgorithm.None;
            TryRead(ref algorithm);

            if (algorithm == CompressionAlgorithm.Deflate)
            {
                _activeStream = new DeflateStream(_stream, CompressionMode.Decompress, true);
            }
        }

        public void BeginCompression(CompressionAlgorithm algorithm)
        {
            Write(ref algorithm);

            if (algorithm == CompressionAlgorithm.Deflate)
            {
                _activeStream = new DeflateStream(_stream, CompressionLevel.SmallestSize, true);
            }
        }

        public void EndCompression()
        {
            if (_activeStream != _stream)
            {
                _activeStream.Dispose();
                _activeStream = _stream;
            }
        }

        public static void ReadCompressed(Stream stream, Span<byte> data)
        {
            CompressionAlgorithm algorithm = (CompressionAlgorithm)stream.ReadByte();

            switch (algorithm)
            {
                case CompressionAlgorithm.None:
                    stream.Read(data);
                    break;
                case CompressionAlgorithm.Deflate:
                    stream = new DeflateStream(stream, CompressionMode.Decompress, true);
                    for (int offset = 0; offset < data.Length;)
                    {
                        offset += stream.Read(data.Slice(offset));
                    }
                    stream.Dispose();
                    break;
            }
        }

        public static void WriteCompressed(Stream stream, ReadOnlySpan<byte> data, CompressionAlgorithm algorithm)
        {
            stream.WriteByte((byte)algorithm);

            switch (algorithm)
            {
                case CompressionAlgorithm.None:
                    stream.Write(data);
                    break;
                case CompressionAlgorithm.Deflate:
                    stream = new DeflateStream(stream, CompressionLevel.SmallestSize, true);
                    stream.Write(data);
                    stream.Dispose();
                    break;
            }
        }
    }
}