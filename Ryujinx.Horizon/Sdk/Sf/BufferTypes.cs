using Ryujinx.Horizon.Kernel;
using Ryujinx.Memory;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Sf
{
    interface IBuffer
    {
        ulong Address { get; }
        ulong Size { get; }
    }

    struct InBuffer<T> : IBuffer where T : unmanaged
    {
        public ulong Address { get; }
        public ulong Size { get; }

        public ReadOnlySpan<T> Span => MemoryMarshal.Cast<byte, T>(KernelStatic.AddressSpace.GetSpan(Address, (int)Size));

        public InBuffer(ulong address, ulong size)
        {
            Address = address;
            Size = size;
        }
    }

    struct OutBuffer<T> : IBuffer, IDisposable where T : unmanaged
    {
        public ulong Address { get; }
        public ulong Size { get; }

        private readonly WritableRegion _region;

        public Span<T> Span => MemoryMarshal.Cast<byte, T>(_region.Memory.Span);

        public OutBuffer(ulong address, ulong size)
        {
            Address = address;
            Size = size;

            _region = KernelStatic.AddressSpace.GetWritableRegion(address, (int)size);
        }

        public void Dispose()
        {
            _region.Dispose();
        }
    }
}
