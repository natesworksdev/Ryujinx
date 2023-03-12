using System;
using System.Buffers.Binary;
using System.IO;

namespace Ryujinx.Common
{
    public static class StreamExtensions
    {
        /// <summary>
        /// Writes a Span of int to this stream.
        ///
        /// This default implementation calls the Write(Object, int, int)
        /// method to write the byte array.
        /// </summary>
        /// <param name="stream">The stream to be written to</param>
        /// <param name="buffer">The buffer of values to be written</param>
        public static void Write(this Stream stream, ReadOnlySpan<int> buffer)
        {
            if (buffer.Length == 0)
                return;

            Span<byte> byteBuffer = stackalloc byte[sizeof(int)];

            foreach (int value in buffer)
            {
                BinaryPrimitives.WriteInt32LittleEndian(byteBuffer, value);
                stream.Write(byteBuffer);
            }
        }

        /// <summary>
        /// Writes a four-byte signed integer to this stream. The current position
        /// of the stream is advanced by four.
        /// </summary>
        /// <param name="stream">The stream to be written to</param>
        /// <param name="value">The value to be written</param>
        public static void Write(this Stream stream, int value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        /// <summary>
        /// Writes an eight-byte signed integer to this stream. The current position
        /// of the stream is advanced by eight.
        /// </summary>
        /// <param name="stream">The stream to be written to</param>
        /// <param name="value">The value to be written</param>
        public static void Write(this Stream stream, long value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        /// <summary>
        // Writes a four-byte unsigned integer to this stream. The current position
        // of the stream is advanced by four.
        /// </summary>
        /// <param name="stream">The stream to be written to</param>
        /// <param name="value">The value to be written</param>
        public static void Write(this Stream stream, uint value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        /// <summary>
        /// Writes an eight-byte unsigned integer to this stream. The current
        /// position of the stream is advanced by eight.
        /// </summary>
        /// <param name="stream">The stream to be written to</param>
        /// <param name="value">The value to be written</param>
        public static void Write(this Stream stream, ulong value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        /// <summary>
        /// Writes the contents of source to stream by calling source.CopyTo(stream).
        /// Provides consistency with other Stream.Write methods.
        /// </summary>
        /// <param name="stream">The stream to be written to</param>
        /// <param name="source">The stream to be read from</param>
        public static void Write(this Stream stream, Stream source)
        {
            source.CopyTo(stream);
        }

        /// <summary>
        /// Writes a sequence of zero bytes to the Stream.
        /// </summary>
        /// <param name="stream">The stream to be written to.</param>
        /// <param name="value">The byte to be written</param>
        /// <param name="count">The number of times the value should be written</param>
        public static void WriteByte(this Stream stream, byte value, int count)
        {
            if (count <= 0)
                return;

            if (count <= 16)
            {
                Span<byte> span = stackalloc byte[count];
                @this.Write(span);
                stream.Write(span);
            }
            else
            {
                // TODO make this better - maybe stackalloc a small Span<byte> and write it in a loop, then handle remainder
                for (int x = 0; x < count; x++)
                {
                    stream.WriteByte(value);
                }
            }
        }
    }
}
