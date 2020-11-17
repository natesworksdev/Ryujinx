using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Graphics.Gpu.Memory
{
    struct MemoryBlock : IComparable<MemoryBlock>
    {
        private ulong address { get; }
        private ulong size { get; }
        private ulong endAddress { get; }

        public MemoryBlock(ulong address, ulong size)
        {
            this.address = address;
            this.size = size;
            this.endAddress = address + size - 1UL;
        }

        public int CompareTo(MemoryBlock other)
        {
            return this.address.CompareTo(other.address);
        }
    }
}
