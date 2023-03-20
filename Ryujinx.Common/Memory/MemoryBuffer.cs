using System;
using System.Buffers;
using System.Threading;

namespace Ryujinx.Common.Memory
{
    /// <summary>
    /// This class simplifies use of <c>System.IO.MemoryPool<byte></c>. That memory pool often returns more
    /// memory than requested, which poses a challenge to code that uses the Length of a given <c>Memory</c>
    /// or <c>Span</c> to know the size of the subject. This class helps by a) simplifying the API for Rent(),
    /// b) implementing the disposable <c>IMemoryOwner</c> interface, and c) tracking the originally requested
    /// length, and only exposing a slice of that length.
    /// </summary>
    public sealed class MemoryBuffer : IMemoryOwner<byte>, IDisposable
    {
        private IMemoryOwner<byte> _memoryOwner;

        private MemoryBuffer(IMemoryOwner<byte> memoryOwner, Memory<byte> memory, int length)
        {
            _memoryOwner = memoryOwner;
            Memory = memory;
            Length = length;
        }

        public static MemoryBuffer Empty = new MemoryBuffer(null, Memory<byte>.Empty, 0);

        /// <summary>
        /// Rents memory from the shared memory pool and returns it in new <c>MemoryBuffer</c> instance.
        /// </summary>
        /// <param name="length">the buffer's required length in bytes</param>
        /// <param name="clear"><c>true</c> to clear the memory after it is rented from the pool. Only the desired length is cleared.</param>
        /// <returns>a new MemoryBuffer instance containing memory rented from <c>MemoryPool<byte>.Shared</c></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static MemoryBuffer Rent(long length, bool clear = false)
        {
            return Rent(checked((int)length), clear);
        }

        /// <summary>
        /// Rents memory from the shared memory pool and returns it in new <c>MemoryBuffer</c> instance.
        /// </summary>
        /// <param name="length">the buffer's required length in bytes</param>
        /// <param name="clear"><c>true</c> to clear the memory after it is rented from the pool. Only the desired length is cleared.</param>
        /// <returns>a new MemoryBuffer instance containing memory rented from <c>MemoryPool<byte>.Shared</c></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static MemoryBuffer Rent(ulong length, bool clear = false)
        {
            return Rent(checked((int)length), clear);
        }

        /// <summary>
        /// Rents memory from the shared memory pool and returns it in new <c>MemoryBuffer</c> instance.
        /// </summary>
        /// <param name="length">the buffer's required length in bytes</param>
        /// <param name="clear"><c>true</c> to clear the memory after it is rented from the pool. Only the desired length is cleared.</param>
        /// <returns>a new MemoryBuffer instance containing memory rented from <c>MemoryPool<byte>.Shared</c></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static MemoryBuffer Rent(int length, bool clear = false)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length cannot be less than 0.");
            }

            if (length == 0)
            {
                return Empty;
            }
            else
            {
                var memoryOwner = MemoryPool<byte>.Shared.Rent(length);

                var memory = length == memoryOwner.Memory.Length
                    ? memoryOwner.Memory
                    : memoryOwner.Memory.Slice(0, length);

                if (clear)
                {
                    memory.Span.Clear();
                }

                return new MemoryBuffer(memoryOwner, memory, length);
            }
        }

        public Memory<byte> Memory { get; }

        public int Length { get; }

        public void Dispose()
        {
            Interlocked.Exchange(ref _memoryOwner, null)?.Dispose();
        }
    }
}
