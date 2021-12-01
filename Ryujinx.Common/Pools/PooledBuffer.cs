using Ryujinx.Common.Logging;
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
    /// This buffer should be disposed when you are done using it, after which it is returned to the pool to be reclaimed.
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

#if TRACK_BUFFERPOOL_LEAKS
        private string _lastRentalStackTrace = "Unknown";

#pragma warning disable CA1063 // suppress "improper" finalizer style
        ~PooledBuffer()
        {
            if (Length > 0)
            {
                Logger.Warning?.Print(LogClass.Application, $"Buffer pool leak detected: Buffer was rented by {_lastRentalStackTrace}");
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
#endif // TRACK_BUFFERPOOL_LEAKS

        /// <summary>
        /// The contiguous buffer to store data in. Intentionally not exposed to callers except as a <see cref="Span{T}"/>, to prevent
        /// the pooled array handle from leaking. The actual array is usually larger than <see cref="Length"/> because of how ArrayPool behaves.
        /// </summary>
        private T[] _buffer;

        /// <summary>
        /// The buffer's allocated length.
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// Returns a pointer to this buffer's data as a span.
        /// </summary>
        public Span<T> AsSpan => _buffer.AsSpan(0, Length);

        /// <summary>
        /// Returns a pointer to this buffer's data as a read-only span.
        /// </summary>
        public ReadOnlySpan<T> AsReadOnlySpan => _buffer.AsSpan(0, Length);

        [SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "Disposal semantics are different here; we are not freeing memory but instead returning objects to a pool")]
        public void Dispose()
        {
            Dispose(true);
#if TRACK_BUFFERPOOL_LEAKS
            GC.SuppressFinalize(this);
#endif
        }

        protected virtual void Dispose(bool disposing)
        {
            // prevent multiple disposal atomically
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
            {
                return;
            }

            if (disposing && Length > 0)
            {
                // if the buffer is zero-length, just keep it zero (this prevents us from corrupting the state of the empty singleton buffer)
                // Otherwise, nullify the reference so anyone who mistakenly still has a handle to this object should hopefully fail fast
                ArrayPool<T>.Shared.Return(_buffer);
                _buffer = null;
                Length = -1;
            }
        }
    }
}
