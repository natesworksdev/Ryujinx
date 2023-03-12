using Microsoft.IO;
using System;
using System.IO;

namespace Ryujinx.Common.Memory
{
    public static class MemoryStreamManager
    {
        private static readonly RecyclableMemoryStreamManager _shared = new RecyclableMemoryStreamManager();

        //public static RecyclableMemoryStreamManager Shared => _shared;

        /// <summary>
        /// Exists until RecyclableMemoryStream version 3x is released, which has API improvements.
        /// </summary>
        public static class Shared
        {

            public static RecyclableMemoryStream GetStream()
                => new RecyclableMemoryStream(_shared);

            public static RecyclableMemoryStream GetStream(byte[] buffer)
                => GetStream(Guid.NewGuid(), null, buffer, 0, buffer.Length);

            public static RecyclableMemoryStream GetStream(ReadOnlySpan<byte> buffer)
                => GetStream(Guid.NewGuid(), null, buffer);

            /// <summary>
            /// Retrieve a new <c>RecyclableMemoryStream</c> object with the given tag and with contents copied from the provided
            /// buffer. The provided buffer is not wrapped or used after construction.
            /// </summary>
            /// <remarks>The new stream's position is set to the beginning of the stream when returned.</remarks>
            /// <param name="id">A unique identifier which can be used to trace usages of the stream.</param>
            /// <param name="tag">A tag which can be used to track the source of the stream.</param>
            /// <param name="buffer">The byte buffer to copy data from.</param>
            /// <returns>A <c>RecyclableMemoryStream</c>.</returns>
            public static RecyclableMemoryStream GetStream(Guid id, string tag, ReadOnlySpan<byte> buffer)
            {
                RecyclableMemoryStream stream = null;
                try
                {
                    stream = new RecyclableMemoryStream(_shared, id, tag, buffer.Length);
                    stream.Write(buffer);
                    stream.Position = 0;
                    return stream;
                }
                catch
                {
                    stream?.Dispose();
                    throw;
                }
            }

            /// <summary>
            /// Retrieve a new <c>RecyclableMemoryStream</c> object with the given tag and with contents copied from the provided
            /// buffer. The provided buffer is not wrapped or used after construction.
            /// </summary>
            /// <remarks>The new stream's position is set to the beginning of the stream when returned.</remarks>
            /// <param name="id">A unique identifier which can be used to trace usages of the stream.</param>
            /// <param name="tag">A tag which can be used to track the source of the stream.</param>
            /// <param name="buffer">The byte buffer to copy data from.</param>
            /// <param name="offset">The offset from the start of the buffer to copy from.</param>
            /// <param name="count">The number of bytes to copy from the buffer.</param>
            /// <returns>A <c>RecyclableMemoryStream</c>.</returns>
            public static RecyclableMemoryStream GetStream(Guid id, string tag, byte[] buffer, int offset, int count)
            {
                RecyclableMemoryStream stream = null;
                try
                {
                    stream = new RecyclableMemoryStream(_shared, id, tag, count);
                    stream.Write(buffer, offset, count);
                    stream.Position = 0;
                    return stream;
                }
                catch
                {
                    stream?.Dispose();
                    throw;
                }
            }

        }
    }
}
