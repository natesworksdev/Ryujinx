using System.Collections.Generic;

namespace Ryujinx.HLE.Memory
{
    internal class ArenaAllocator
    {
        private class Region
        {
            public long Position { get; set; }
            public long Size     { get; set; }

            public Region(long position, long size)
            {
                Position = position;
                Size     = size;
            }
        }

        private LinkedList<Region> _freeRegions;

        public long TotalAvailableSize { get; private set; }
        public long TotalUsedSize      { get; private set; }

        public ArenaAllocator(long arenaSize)
        {
            TotalAvailableSize = arenaSize;

            _freeRegions = new LinkedList<Region>();

            _freeRegions.AddFirst(new Region(0, arenaSize));
        }

        public bool TryAllocate(long size, out long position)
        {
            LinkedListNode<Region> node = _freeRegions.First;

            while (node != null)
            {
                Region rg = node.Value;

                if ((ulong)rg.Size >= (ulong)size)
                {
                    position = rg.Position;

                    rg.Position += size;
                    rg.Size     -= size;

                    if (rg.Size == 0)
                    {
                        //Region is empty, just remove it.
                        _freeRegions.Remove(node);
                    }
                    else if (node.Previous != null)
                    {
                        //Re-sort based on size (smaller first).
                        node = node.Previous;

                        _freeRegions.Remove(node.Next);

                        while (node != null && (ulong)node.Value.Size > (ulong)rg.Size)
                        {
                            node = node.Previous;
                        }

                        if (node != null)
                        {
                            _freeRegions.AddAfter(node, rg);
                        }
                        else
                        {
                            _freeRegions.AddFirst(rg);
                        }
                    }

                    TotalUsedSize += size;

                    return true;
                }

                node = node.Next;
            }

            position = 0;

            return false;
        }

        public void Free(long position, long size)
        {
            long end = position + size;

            Region newRg = new Region(position, size);

            LinkedListNode<Region> node   = _freeRegions.First;
            LinkedListNode<Region> prevSz = null;

            while (node != null)
            {
                LinkedListNode<Region> nextNode = node.Next;

                Region rg = node.Value;

                long rgEnd = rg.Position + rg.Size;

                if (rg.Position == end)
                {
                    //Current region position matches the end of the freed region,
                    //just merge the two and remove the current region from the list.
                    newRg.Size += rg.Size;

                    _freeRegions.Remove(node);
                }
                else if (rgEnd == position)
                {
                    //End of the current region matches the position of the freed region,
                    //just merge the two and remove the current region from the list.
                    newRg.Position  = rg.Position;
                    newRg.Size     += rg.Size;

                    _freeRegions.Remove(node);
                }
                else
                {
                    if (prevSz == null)
                    {
                        prevSz = node;
                    }
                    else if ((ulong)rg.Size < (ulong)newRg.Size &&
                             (ulong)rg.Size > (ulong)prevSz.Value.Size)
                    {
                        prevSz = node;
                    }
                }

                node = nextNode;
            }

            if (prevSz != null && (ulong)prevSz.Value.Size < (ulong)size)
            {
                _freeRegions.AddAfter(prevSz, newRg);
            }
            else
            {
                _freeRegions.AddFirst(newRg);
            }

            TotalUsedSize -= size;
        }
    }
}