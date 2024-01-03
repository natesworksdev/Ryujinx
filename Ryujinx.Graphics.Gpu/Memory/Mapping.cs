using Ryujinx.Memory.Range;

namespace Ryujinx.Graphics.Gpu.Memory
{
    struct Mapping : IRange
    {
        public ulong Address { get; }
        public ulong Size { get; }
        public ulong EndAddress => Address + Size;

        public bool OverlapsWith(ulong address, ulong size)
        {
            return Address < address + size && address < EndAddress;
        }

        public Mapping(ulong address, ulong size)
        {
            Address = address;
            Size = size;
        }
    }
}