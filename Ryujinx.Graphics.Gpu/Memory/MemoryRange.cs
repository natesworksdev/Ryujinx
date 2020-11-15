using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Ryujinx.Graphics.Gpu.Memory
{
    struct MemoryRange : IEquatable<MemoryRange>, IComparable<MemoryRange>
    {
        public ulong startAddress { get; }

        public ulong endAddress { get; }
        
        public ulong size { get; }

        public static MemoryRange InvalidRange = new MemoryRange(0, 0);

        public MemoryRange(ulong address, ulong endAddress)
        {
            this.startAddress = address;
            this.endAddress = endAddress;
            this.size = endAddress - address;
        }

        public override bool Equals(object obj)
        {
            return Equals((MemoryRange)obj);
        }

        public bool Equals(MemoryRange other)
        {
            return 
                   startAddress == other.startAddress &&
                   endAddress == other.endAddress;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(startAddress, endAddress);
        }

        public int CompareTo(MemoryRange other)
        {
            return this.startAddress.CompareTo(other.startAddress);
        }

        public override string ToString()
        {
            return $" [{startAddress} -> {endAddress}] ";
        }
    }
}
