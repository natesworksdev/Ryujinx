using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Kernel
{
    class KSlabHeap
    {
        private LinkedList<long> Items;

        public KSlabHeap(long Pa, long ItemSize, long Size)
        {
            Items = new LinkedList<long>();

            int ItemsCount = (int)(Size / ItemSize);

            for (int Index = 0; Index < ItemsCount; Index++)
            {
                Items.AddLast(Pa);

                Pa += ItemSize;
            }
        }

        public bool TryGetItem(out long Pa)
        {
            lock (Items)
            {
                if (Items.First != null)
                {
                    Pa = Items.First.Value;

                    Items.RemoveFirst();

                    return true;
                }
            }

            Pa = 0;

            return false;
        }

        public void Free(long Pa)
        {
            lock (Items)
            {
                Items.AddFirst(Pa);
            }
        }
    }
}