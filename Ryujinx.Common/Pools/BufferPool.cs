using System;
using System.Buffers;

namespace Ryujinx.Common.Pools
{
    /// <summary>
    /// A centralized, static pool for getting multipurpose buffers of any type.
    /// "But why does this exist when you have <see cref="ArrayPool{T}"/>?" This actually just wraps around
    /// ArrayPool. The difference is that this also tracks the requested size of the allocated
    /// buffer, so you can pass it around and users of it know how much of the data is actually valid,
    /// compared to ArrayPool which just gives you an arbitrary sized array. Also, returning a strongly
    /// typed <see cref="PooledBuffer{T}"/> codifies the fact that the buffer is part of a pool, and that code
    /// can more clearly manage lifetime and disposal automatically.
    /// </summary>
    public static class BufferPool<T>
    {
        public const int DEFAULT_BUFFER_SIZE = 65536;

        private static readonly PooledBuffer<T> EMPTY_POOLED_BUFFER = new PooledBuffer<T>(Array.Empty<T>(), -1);

        /// <summary>
        /// Returns a disposable <see cref="PooledBuffer{T}" /> instance backed by a contiguous array of at least the requested number of elements.
        /// The internal array is only exposed as a <see cref="Span{T}"/> to prevent user code from storing references to it outside
        /// of the potential lifetime of the pooled handle.
        /// </summary>
        /// <param name="minimumRequestedSize">The minimum number of elements that you require</param>
        /// <param name="clearArray">Whether to clear the contents of the buffer before returning. If false (by default), the buffer may initially contain garbage.</param>
        /// <returns>A pooled buffer of exactly the requested size.</returns>
        public static PooledBuffer<T> Rent(int minimumRequestedSize = DEFAULT_BUFFER_SIZE, bool clearArray = false)
        {
            if (minimumRequestedSize < 0)
            {
                throw new ArgumentOutOfRangeException("Requested buffer size must be a positive number");
            }

            if (minimumRequestedSize == 0)
            {
                return EMPTY_POOLED_BUFFER;
            }

            T[] array = ArrayPool<T>.Shared.Rent(minimumRequestedSize);
            if (clearArray)
            {
                Array.Clear(array, 0, minimumRequestedSize);
            }

            PooledBuffer<T> returnVal = new PooledBuffer<T>(array, minimumRequestedSize);
#if DEBUG
            returnVal.MarkRented();
#endif
            return returnVal;
        }
    }
}