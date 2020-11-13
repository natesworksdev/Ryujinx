using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Ryujinx.Graphics.Gpu.Memory
{
    class MemoryRange : IEquatable<MemoryRange>, IComparable<MemoryRange>
    {
        public ulong startAddress { get; }

        public ulong endAddress { get; }
        public MemoryRange(ulong address, ulong endAddress)
        {
            this.startAddress = address;
            this.endAddress = endAddress;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MemoryRange);
        }

        public bool Equals(MemoryRange other)
        {
            return other != null &&
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
            return $" |{startAddress} -> {endAddress}| ";
        }
    }
}
