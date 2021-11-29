using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;

namespace Ryujinx.Common.Pools
{
    /// <summary>
    /// Represents a reusable pooled buffer of some type T that was rented from a <see cref="BufferPool{T}"/>.
    /// The actual length of the array may be longer than the actual valid contents; to codify the difference, the Length
    /// parameter is used (in cases where you pass a buffered pool to some function like a reader that parses data from it).
    /// This pool should be disposed when you are done using it, after which it is returned to the pool to be reclaimed.
    /// </summary>
    /// <typeparam name="T">The type of data that this buffer holds.</typeparam>
    public class PooledBuffer<T> : IDisposable
    {
        private int _disposed = 0;

        internal PooledBuffer(T[] data, int length)
        {
            _buffer = data;
            Length = length;
        }

#if DEBUG
        private string _lastRentalStackTrace = "Unknown";

#pragma warning disable CA1063 // suppress "improper" finalizer style
        ~PooledBuffer()
        {
            if (Length > 0)
            {
                Console.WriteLine("Buffer pool leak detected: Buffer was rented by " + _lastRentalStackTrace);
                Dispose(false);
            }
        }
#pragma warning restore CA1063

        internal void MarkRented()
        {
            string stackTrace = new StackTrace().ToString();
            string objectTypeName = nameof(BufferPool<T>);
            string traceLine;
            int startIdx = 0;
            while (startIdx < stackTrace.Length)
            {
                startIdx = stackTrace.IndexOf('\n', startIdx);
                if (startIdx < 0)
                {
                    break;
                }

                startIdx += 7; // trim the "   at " prefix

                int endIdx = stackTrace.IndexOf('\n', startIdx) - 1;
                if (endIdx < 0)
                {
                    endIdx = stackTrace.Length;
                }

                traceLine = stackTrace.Substring(startIdx, endIdx - startIdx);
                if (!traceLine.Contains(objectTypeName))
                {
                    _lastRentalStackTrace = traceLine;
                    return;
                }
            }
        }
#endif // DEBUG

        /// <summary>
        /// The contiguous buffer to store data in.
        /// </summary>
        private T[] _buffer;

        /// <summary>
        /// The buffer's intended length. This may be smaller than what is actually available (since a larger pooled buffer may have been used
        /// to satisfy the request). This can be used as a signal to indicate the intended data size this buffer is used for.
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// Returns a pointer to this buffer's data as a span.
        /// </summary>
        public Span<T> AsSpan => _buffer[0..Length];

        /// <summary>
        /// Returns a pointer to this buffer's data as a read-only span.
        /// </summary>
        public ReadOnlySpan<T> AsReadOnlySpan => _buffer[0..Length];

        [SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "Disposal semantics are different here; we are not freeing memory but instead returning objects to a pool")]
        public void Dispose()
        {
            Dispose(true);
#if DEBUG
            GC.SuppressFinalize(this);
#endif
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
            {
                return;
            }

            if (disposing && Length > 0)
            {
                // if the buffer is zero-length, just keep it zero (this prevents us from corrupting the state of the empty singleton buffer)
                ArrayPool<T>.Shared.Return(_buffer);
                _buffer = null;
                Length = -1;
            }
        }
    }
}
