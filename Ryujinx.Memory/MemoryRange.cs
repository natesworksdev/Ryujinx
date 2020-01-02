using Ryujinx.Common;
using System;

using static Ryujinx.Memory.MemoryConstants;

namespace Ryujinx.Memory
{
    /// <summary>
    /// Represents a write tracked range of memory.
    /// </summary>
    public class MemoryRange
    {
        private readonly MemoryBlock _memoryBlock;

        /// <summary>
        /// Address of the memory range.
        /// </summary>
        public ulong Address { get; }

        /// <summary>
        /// Size in bytes of the memory range.
        /// </summary>
        public ulong Size { get; }

        private readonly ulong _lastPageAddress;

        private readonly int _firstPage;
        private readonly int _firstPageOffset;
        private readonly int _lastPage;
        private readonly int _lastPageEndOffset;

        private readonly byte[] _firstPageData;
        private readonly byte[] _lastPageData;

        /// <summary>
        /// Creates a new instance of the memory range class.
        /// </summary>
        /// <param name="memoryBlock">Parent memory block that the range belongs to</param>
        /// <param name="address">Start address of the range</param>
        /// <param name="size">Size in bytes of the range</param>
        internal MemoryRange(MemoryBlock memoryBlock, ulong address, ulong size)
        {
            _memoryBlock = memoryBlock;
            Address      = address;
            Size         = size;

            int firstPage       = (int)(address / PageSize);
            int firstPageOffset = (int)(address & PageMask);

            if (firstPageOffset != 0 || size < PageSize)
            {
                int dataSize = PageSize - firstPageOffset;

                if ((ulong)dataSize > size)
                {
                    dataSize = (int)size;
                }

                _firstPageData = new byte[dataSize];

                _memoryBlock.Read(address, _firstPageData);
            }

            ulong endAddress = address + size;

            int lastPage = (int)(BitUtils.DivRoundUp(endAddress, PageSize) - 1);

            int lastPageEndOffset = (int)(endAddress & PageMask);

            ulong lastPageAddress = endAddress - (ulong)lastPageEndOffset;

            if (lastPage > firstPage && lastPageEndOffset != 0)
            {
                _lastPageData = new byte[lastPageEndOffset];

                _memoryBlock.Read(lastPageAddress, _lastPageData);
            }

            ulong addressAligned = BitUtils.AlignUp(address, PageSize);

            int alignedPagesCount = (int)((BitUtils.AlignDown(endAddress, PageSize) - addressAligned) >> PageBits);

            memoryBlock.QueryModified((int)(addressAligned >> PageBits), alignedPagesCount, this);

            _lastPageAddress   = lastPageAddress;
            _firstPage         = firstPage;
            _firstPageOffset   = firstPageOffset;
            _lastPage          = lastPage;
            _lastPageEndOffset = lastPageEndOffset;
        }

        /// <summary>
        /// Checks if the memory range was written since the last call to this method.
        /// </summary>
        /// <returns>True if the range data was modified since the last call, false otherwise</returns>
        public bool QueryModified()
        {
            bool modified = false;

            int page = _firstPage;

            if (_firstPageOffset != 0 || Size < PageSize)
            {
                int dataSize = _firstPageData.Length;

                if ((ulong)dataSize > Size)
                {
                    dataSize = (int)Size;
                }

                Span<byte> oldData = new Span<byte>(_firstPageData);

                ReadOnlySpan<byte> newData = _memoryBlock.GetSpan(Address, dataSize);

                if (!oldData.SequenceEqual(newData))
                {
                    modified = true;

                    newData.CopyTo(oldData);
                }

                page++;
            }

            int endPage = _lastPageEndOffset != 0 ? _lastPage : _lastPage + 1;

            int alignedPagesCount = endPage - page;

            if (alignedPagesCount > 0 && _memoryBlock.QueryModified(page, alignedPagesCount, this))
            {
                modified = true;
            }

            if (_lastPage > _firstPage && _lastPageEndOffset != 0)
            {
                Span<byte> oldData = new Span<byte>(_lastPageData);

                ReadOnlySpan<byte> newData = _memoryBlock.GetSpan(_lastPageAddress, _lastPageData.Length);

                if (!oldData.SequenceEqual(newData))
                {
                    modified = true;

                    newData.CopyTo(oldData);
                }
            }

            return modified;
        }

        /// <summary>
        /// Gets a span of data inside this memory range.
        /// </summary>
        /// <remarks>
        /// Use this method after checking for data modification inside the memory range.
        /// This ensures that you're working with the same data being used internally
        /// for modification checks with memory comparison.
        /// </remarks>
        /// <returns>The data span</returns>
        public Span<byte> GetSpan()
        {
            if (_firstPageData == null && _lastPageData == null)
            {
                return _memoryBlock.GetSpan(Address, (int)Size);
            }

            Span<byte> data = new byte[Size];

            int middleOffset = 0;
            int unalignedSize = 0;

            if (_firstPageData != null)
            {
                middleOffset = _firstPageData.Length;
                unalignedSize = _firstPageData.Length;

                _firstPageData.CopyTo(data.Slice(0, _firstPageData.Length));
            }

            if (_lastPageData != null)
            {
                unalignedSize += _lastPageData.Length;

                _lastPageData.CopyTo(data.Slice(data.Length - _lastPageData.Length));
            }

            int middleSize = data.Length - unalignedSize;

            _memoryBlock.Read(Address + (ulong)middleOffset, data.Slice(middleOffset, middleSize));

            return data;
        }

        /// <summary>
        /// Checks if a given sub-range inside the memory range was written since the last
        /// call to this method.
        /// </summary>
        /// <param name="offset">Offset of the sub-range being checked</param>
        /// <param name="size">Size in bytes of the sub-range being checked</param>
        /// <param name="data">Buffer used for memory comparison, and to hold the updated data</param>
        /// <returns>True if the sub-range was modified, false otherwise</returns>
        public bool QueryModified(int offset, int size, Span<byte> data)
        {
            ulong address = Address + (ulong)offset;

            int firstPage       = (int)(address / PageSize);
            int firstPageOffset = (int)(address & PageMask);

            ulong endAddress = address + (ulong)size;

            int lastPage = (int)(BitUtils.DivRoundUp(endAddress, PageSize) - 1);

            int lastPageEndOffset = (int)(endAddress & PageMask);

            bool modified = false;

            int page = firstPage;

            int alignedOffset = offset;

            if (firstPageOffset != 0 || size < PageSize)
            {
                int dataSize = PageSize - firstPageOffset;

                if (dataSize > size)
                {
                    dataSize = size;
                }

                alignedOffset += dataSize;

                Span<byte> oldData = data.Slice(offset, dataSize);

                ReadOnlySpan<byte> newData = _memoryBlock.GetSpan(address, dataSize);

                if (!oldData.SequenceEqual(newData))
                {
                    modified = true;

                    newData.CopyTo(oldData);
                }

                page++;
            }

            int endPage = lastPageEndOffset != 0 ? lastPage : lastPage + 1;

            int alignedPagesCount = endPage - page;

            if (alignedPagesCount > 0)
            {
                Span<byte> outBuffer = data.Slice(alignedOffset, alignedPagesCount << PageBits);

                if (_memoryBlock.QueryModified(page, alignedPagesCount, this, outBuffer))
                {
                    modified = true;
                }
            }

            if (lastPage > firstPage && lastPageEndOffset != 0)
            {
                Span<byte> oldData = data.Slice(offset + size - lastPageEndOffset, lastPageEndOffset);

                ReadOnlySpan<byte> newData = _memoryBlock.GetSpan(endAddress - (ulong)lastPageEndOffset, lastPageEndOffset);

                if (!oldData.SequenceEqual(newData))
                {
                    modified = true;

                    newData.CopyTo(oldData);
                }
            }

            return modified;
        }
    }
}
