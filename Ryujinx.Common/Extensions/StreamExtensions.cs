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
        /// <param name="this"></param>
        /// <param name="buffer"></param>
        public static void Write(this Stream @this, ReadOnlySpan<int> buffer)
        {
            if (buffer.Length == 0)
                return;

            Span<byte> byteBuffer = stackalloc byte[sizeof(int)];

            foreach (int value in buffer)
            {
                BinaryPrimitives.WriteInt32LittleEndian(byteBuffer, value);
                @this.Write(byteBuffer);
            }
        }

        /// <summary>
        /// Writes a four-byte signed integer to this stream. The current position
        /// of the stream is advanced by four.
        /// </summary>
        /// <param name="this"></param>
        /// <param name="value"></param>
        public static void Write(this Stream @this, int value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            @this.Write(buffer);
        }

        /// <summary>
        /// Writes an eight-byte signed integer to this stream. The current position
        /// of the stream is advanced by eight.
        /// </summary>
        /// <param name="this"></param>
        /// <param name="value"></param>
        public static void Write(this Stream @this, long value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
            @this.Write(buffer);
        }

        /// <summary>
        // Writes a four-byte unsigned integer to this stream. The current position
        // of the stream is advanced by four.
        /// </summary>
        /// <param name="this"></param>
        /// <param name="value"></param>
        public static void Write(this Stream @this, uint value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
            @this.Write(buffer);
        }

        /// <summary>
        /// Writes an eight-byte unsigned integer to this stream. The current
        /// position of the stream is advanced by eight.
        /// </summary>
        /// <param name="this"></param>
        /// <param name="value"></param>
        public static void Write(this Stream @this, ulong value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
            @this.Write(buffer);
        }

        /// <summary>
        /// Writes the contents of stream to this, by calling stream.CopyTo(@this).
        /// Exists to provide consistency with other MemoryStream.Write methods.
        /// </summary>
        /// <param name="this">the stream that will be written to</param>
        /// <param name="source">the stream that will be read from</param>
        public static void Write(this Stream @this, Stream source)
        {
            source.CopyTo(@this);
        }

        /// <summary>
        /// Writes a sequence of zero bytes to the Stream.
        /// </summary>
        /// <param name="this"></param>
        /// <param name="length"></param>
        public static void WriteByte(this Stream @this, byte value, int count)
        {
            if (count <= 0)
                return;

            if (count <= 16)
            {
                Span<byte> span = stackalloc byte[count];
                @this.Write(span);
            }
            else
            {
                // TODO make this better - maybe stackalloc a small Span<byte> and write it in a loop, then handle remainder
                for (int x = 0; x < count; x++)
                {
                    @this.WriteByte(value);
                }
            }
        }
    }
}
