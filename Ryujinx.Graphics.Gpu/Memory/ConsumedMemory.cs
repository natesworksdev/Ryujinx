using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Ryujinx.Graphics.Gpu.Memory
{
    class ConsumedMemory : IEquatable<ConsumedMemory>, IComparable<ConsumedMemory>
    {
        public ulong address { get; }

        public ulong endAddress { get; }
        public ConsumedMemory(ulong address, ulong size)
        {
            this.address = address;
            this.endAddress = address + size;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ConsumedMemory);
        }

        public bool Equals(ConsumedMemory other)
        {
            return other != null &&
                   address == other.address &&
                   endAddress == other.endAddress;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(address, endAddress);
        }

        public int CompareTo(ConsumedMemory other)
        {
            return this.address.CompareTo(other.address);
        }
    }
}
