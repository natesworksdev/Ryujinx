using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Graphics.Gpu.Memory
{
    class MemoryBlockManager
    {
        private const int PtLvl0Bits = 14;
        private const int PtLvl1Bits = 14;
        private const int PtPageBits = 12;

        private const ulong PtLvl0Size = 1UL << PtLvl0Bits;
        private const ulong PtLvl1Size = 1UL << PtLvl1Bits;
        private const ulong PageSize = 1UL << PtPageBits;

        private const ulong PtLvl0Mask = PtLvl0Size - 1;
        private const ulong PtLvl1Mask = PtLvl1Size - 1;
        private const ulong PageMask = PageSize - 1;

        private const int PtLvl0Bit = PtPageBits + PtLvl1Bits;
        private const int PtLvl1Bit = PtPageBits;

        private const ulong PteUnmapped = 0xffffffff_ffffffff;
        private const ulong PteReserved = 0xffffffff_fffffffe;

        private LinkedList<MemoryBlock> _list = new LinkedList<MemoryBlock>();
        private TreeMap<ulong, MemoryBlock> _map = new TreeMap<ulong, MemoryBlock>();
        public MemoryBlockManager(ulong maxAddressSize)
        {
            _list.AddFirst(new MemoryBlock(1UL, maxAddressSize));
            _map.Put(1UL, _list.First.Value);
        }

        public ulong FindFreeAddress(ulong size, ulong alignment, ulong start)
        {
            return -1ul;
        }

        public void Allocate(ulong address, ulong size)
        {
            lock(_list) {
                lock(_map)
                {

                }
            }
        }

        public void Deallocate(ulong address)
        {
            lock (_list)
            {
                lock (_map)
                {

                }
            }
        }
    }
}
