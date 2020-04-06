using ARMeilleure.Memory;
using Ryujinx.Memory;
using System;

namespace Ryujinx.Tests.Cpu
{
    class MemoryBlockWrapper : IMemoryBlock
    {
        private readonly MemoryBlock _impl;

        public IntPtr Pointer => _impl.Pointer;

        public MemoryBlockWrapper(ulong size, MemoryAllocationFlags flags = MemoryAllocationFlags.None)
        {
            _impl = new MemoryBlock(size, flags);
        }

        public bool Commit(ulong offset, ulong size) => _impl.Commit(offset, size);
        public IntPtr GetPointer(ulong offset, int size) => _impl.GetPointer(offset, size);
        public ref T GetRef<T>(ulong offset) where T : unmanaged => ref _impl.GetRef<T>(offset);
        public Span<byte> GetSpan(ulong offset, int size) => _impl.GetSpan(offset, size);
        public void MapAsRx(ulong offset, ulong size) => _impl.Reprotect(offset, size, MemoryPermission.ReadAndExecute);
        public void MapAsRwx(ulong offset, ulong size) => _impl.Reprotect(offset, size, MemoryPermission.ReadWriteExecute);
        public T Read<T>(ulong offset) where T : unmanaged => _impl.Read<T>(offset);
        public void Write<T>(ulong offset, T value) where T : unmanaged => _impl.Write(offset, value);

        public void Dispose() => _impl.Dispose();
    }
}
