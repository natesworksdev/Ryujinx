using Ryujinx.Memory;
using System;
using System.Threading;

namespace Ryujinx.Cpu.AppleHv
{
    static class HvVm
    {
        // This alignment allows us to use larger blocks on the page table.
        private const ulong AsIpaAlignment = 1UL << 30;

        private static int _addressSpaces;
        private static HvIpaAllocator _ipaAllocator;

        public static (ulong, HvIpaAllocator) CreateAddressSpace(MemoryBlock block)
        {
            if (Interlocked.Increment(ref _addressSpaces) == 1)
            {
                HvApi.hv_vm_create(IntPtr.Zero).ThrowOnError();
                _ipaAllocator = new HvIpaAllocator();
            }

            ulong baseAddress;

            lock (_ipaAllocator)
            {
                baseAddress = _ipaAllocator.Allocate(block.Size, AsIpaAlignment);
            }

            var rwx = hv_memory_flags_t.HV_MEMORY_READ | hv_memory_flags_t.HV_MEMORY_WRITE | hv_memory_flags_t.HV_MEMORY_EXEC;

            HvApi.hv_vm_map((ulong)block.Pointer, baseAddress, block.Size, rwx).ThrowOnError();

            return (baseAddress, _ipaAllocator);
        }

        public static void DestroyAddressSpace(ulong address, ulong size)
        {
            HvApi.hv_vm_unmap(address, size);

            lock (_ipaAllocator)
            {
                _ipaAllocator.Free(address, size);
            }

            if (Interlocked.Decrement(ref _addressSpaces) == 0)
            {
                HvApi.hv_vm_destroy().ThrowOnError();
            }
        }
    }
}