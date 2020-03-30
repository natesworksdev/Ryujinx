using ARMeilleure.Memory;
using Ryujinx.Memory;
using System;

namespace Ryujinx.HLE
{
    class MemoryBlockWrapper : IMemoryBlock
    {
        private readonly MemoryBlock _impl;

        public MemoryBlock Impl => _impl;

        public IntPtr Pointer => _impl.Pointer;

        public MemoryBlockWrapper(ulong size)
        {
            _impl = new MemoryBlock(size);
        }

        public void Copy(ulong srcAddress, ulong dstAddress, ulong size) => _impl.Copy(srcAddress, dstAddress, size);
        public IntPtr GetPointer(ulong address, int size) => _impl.GetPointer(address, size);
        public ref T GetRef<T>(ulong address) where T : unmanaged => ref _impl.GetRef<T>(address);
        public Span<byte> GetSpan(ulong address, int size) => _impl.GetSpan(address, size);
        public T Read<T>(ulong address) where T : unmanaged => _impl.Read<T>(address);
        public void Write<T>(ulong address, T value) where T : unmanaged => _impl.Write(address, value);
        public void ZeroFill(ulong address, ulong size) => _impl.ZeroFill(address, size);

        public void Dispose() => _impl.Dispose();
    }
}
