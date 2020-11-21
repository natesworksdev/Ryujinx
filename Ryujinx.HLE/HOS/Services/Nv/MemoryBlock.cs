using System;

namespace Ryujinx.HLE.HOS.Services.Nv
{
    internal struct MemoryBlock : IComparable<MemoryBlock>
    {
        public ulong Address { get; }
        public ulong Size { get; }
        public ulong EndAddress { get; }

        public MemoryBlock(ulong address, ulong size)
        {
            this.Address = address;
            this.Size = size;
            this.EndAddress = address + size;
        }

        public int CompareTo(MemoryBlock other)
        {
            return this.Address.CompareTo(other.Address);
        }

        public override string ToString()
        {
            return $" [{Address} - ({Size}) -> {EndAddress}] ";
        }
    }
}
