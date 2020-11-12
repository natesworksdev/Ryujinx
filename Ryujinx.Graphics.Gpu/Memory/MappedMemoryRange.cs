using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Ryujinx.Graphics.Gpu.Memory
{
    class MappedMemoryRange : IEquatable<MappedMemoryRange>, IComparable<MappedMemoryRange>
    {
        public ulong startAddress { get; }

        public ulong endAddress { get; }
        public MappedMemoryRange(ulong address, ulong size)
        {
            this.startAddress = address;
            this.endAddress = address + size;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MappedMemoryRange);
        }

        public bool Equals(MappedMemoryRange other)
        {
            return other != null &&
                   startAddress == other.startAddress &&
                   endAddress == other.endAddress;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(startAddress, endAddress);
        }

        public int CompareTo(MappedMemoryRange other)
        {
            return this.startAddress.CompareTo(other.startAddress);
        }
    }
}
