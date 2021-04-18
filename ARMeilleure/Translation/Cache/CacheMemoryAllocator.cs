using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ARMeilleure.Translation.Cache
{
    class CacheMemoryAllocator
    {
        private struct MemoryBlock : IComparable<MemoryBlock>
        {
            public uint Offset { get; }
            public uint Size { get; }

            public MemoryBlock(uint offset, uint size)
            {
                Offset = offset;
                Size = size;
            }

            public int CompareTo([AllowNull] MemoryBlock other)
            {
                return Offset.CompareTo(other.Offset);
            }
        }

        private readonly List<MemoryBlock> _blocks = new List<MemoryBlock>();

        public CacheMemoryAllocator(uint capacity)
        {
            _blocks.Add(new MemoryBlock(0, capacity));
        }

        public bool TryAllocate(uint size, out uint offset)
        {
            for (int i = 0; i < _blocks.Count; i++)
            {
                MemoryBlock block = _blocks[i];

                if (block.Size > size)
                {
                    _blocks[i] = new MemoryBlock(block.Offset + size, block.Size - size);

                    offset = block.Offset;
                    return true;
                }
                else if (block.Size == size)
                {
                    _blocks.RemoveAt(i);

                    offset = block.Offset;
                    return true;
                }
            }

            offset = default;
            return false;
        }

        public void Free(uint offset, uint size)
        {
            Insert(new MemoryBlock(offset, size));
        }

        private void Insert(MemoryBlock block)
        {
            int index = _blocks.BinarySearch(block);

            if (index < 0)
            {
                index = ~index;
            }

            if (index < _blocks.Count)
            {
                MemoryBlock next = _blocks[index];

                uint endOffs = block.Offset + block.Size;

                if (next.Offset == endOffs)
                {
                    block = new MemoryBlock(block.Offset, block.Size + next.Size);
                    _blocks.RemoveAt(index);
                }
            }

            if (index > 0)
            {
                MemoryBlock prev = _blocks[index - 1];

                if (prev.Offset + prev.Size == block.Offset)
                {
                    block = new MemoryBlock(block.Offset - prev.Size, block.Size + prev.Size);
                    _blocks.RemoveAt(--index);
                }
            }

            _blocks.Insert(index, block);
        }
    }
}
