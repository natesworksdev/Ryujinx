using ARMeilleure.Memory;
using Ryujinx.Common;
using Ryujinx.Memory;
using System;

namespace Ryujinx.Cpu
{
    class JitMemoryBlock : DisposableBase, IJitMemoryBlock
    {
        private readonly MemoryBlock _impl;

        public IntPtr Pointer => _impl.Pointer;

        public JitMemoryBlock(ulong size, MemoryAllocationFlags flags)
        {
            _impl = new MemoryBlock(size, flags);
        }

        public bool Commit(ulong offset, ulong size) => _impl.Commit(offset, size);
        public void MapAsRx(ulong offset, ulong size) => _impl.Reprotect(offset, size, MemoryPermission.ReadAndExecute);
        public void MapAsRwx(ulong offset, ulong size) => _impl.Reprotect(offset, size, MemoryPermission.ReadWriteExecute);

        protected override void DisposeManaged()
        {
            _impl.Dispose();
        }
    }
}
