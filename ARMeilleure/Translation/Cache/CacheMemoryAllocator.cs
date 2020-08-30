using System.Collections.Generic;

namespace ARMeilleure.Translation.Cache
{
    class CacheMemoryAllocator
    {
        private struct MemoryBlock
        {
            public int Offset;
            public int Size;

            public MemoryBlock(int offset, int size)
            {
                Offset = offset;
                Size = size;
            }
        }

        private readonly LinkedList<MemoryBlock> _blocks = new LinkedList<MemoryBlock>();

        public CacheMemoryAllocator(int capacity)
        {
            _blocks.AddFirst(new MemoryBlock(0, capacity));
        }

        public int Allocate(int size)
        {
            for (LinkedListNode<MemoryBlock> node = _blocks.First; node != null; node = node.Next)
            {
                MemoryBlock block = node.Value;

                if (block.Size > size)
                {
                    int offset = block.Offset;
                    block.Offset+= size;
                    block.Size -= size;
                    node.Value = block;
                    return offset;
                }
                else if (block.Size == size)
                {
                    _blocks.Remove(node);
                    return block.Offset;
                }
            }

            // We don't have enough free memory to perform the allocation.
            return -1;
        }

        public void Free(int offset, int size)
        {
            if (!TryCoalesce(offset, size))
            {
                for (LinkedListNode<MemoryBlock> node = _blocks.First; node != null; node = node.Next)
                {
                    MemoryBlock block = node.Value;

                    if (block.Size <= size)
                    {
                        _blocks.AddBefore(node, new MemoryBlock(offset, size));
                        break;
                    }
                    else if (node.Next == null)
                    {
                        _blocks.AddLast(new MemoryBlock(offset, size));
                    }
                }
            }
        }

        private bool TryCoalesce(int offset, int size)
        {
            int freedEnd = offset + size;

            for (LinkedListNode<MemoryBlock> node = _blocks.First; node != null; node = node.Next)
            {
                MemoryBlock block = node.Value;

                int end = block.Offset + block.Size;

                if (end == offset)
                {
                    block.Size += size;
                    node.Value = block;
                    return true;
                }
                else if (freedEnd == block.Offset)
                {
                    block.Offset -= size;
                    block.Size += size;
                    node.Value = block;
                    return true;
                }
            }

            return false;
        }
    }
}
