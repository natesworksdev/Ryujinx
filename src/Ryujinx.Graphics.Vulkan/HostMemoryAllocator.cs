using Ryujinx.Common;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ryujinx.Graphics.Vulkan
{
    internal class HostMemoryAllocator
    {
        private struct HostMemoryAllocation
        {
            public readonly MemoryAllocation Allocation;
            public readonly IntPtr Pointer;
            public readonly ulong Size;

            public HostMemoryAllocation(MemoryAllocation allocation, IntPtr pointer, ulong size)
            {
                Allocation = allocation;
                Pointer = pointer;
                Size = size;
            }
        }

        private readonly MemoryAllocator _allocator;
        private readonly Vk _api;
        private readonly Device _device;

        private List<HostMemoryAllocation> _allocations;

        public HostMemoryAllocator(MemoryAllocator allocator, Vk api, Device device)
        {
            _allocator = allocator;
            _api = api;
            _device = device;

            _allocations = new List<HostMemoryAllocation>();
        }

        public unsafe bool TryImport(
            MemoryRequirements requirements,
            MemoryPropertyFlags flags,
            IntPtr pointer,
            ulong size)
        {
            int memoryTypeIndex = _allocator.FindSuitableMemoryTypeIndex(requirements.MemoryTypeBits, flags);
            if (memoryTypeIndex < 0)
            {
                return default;
            }

            nint pageAlignedPointer = BitUtils.AlignDown(pointer, Environment.SystemPageSize);
            nint pageAlignedEnd = BitUtils.AlignUp((nint)((ulong)pointer + size), Environment.SystemPageSize);
            ulong pageAlignedSize = (ulong)(pageAlignedEnd - pageAlignedPointer);
            ulong offset = (ulong)(pointer - pageAlignedPointer);

            ImportMemoryHostPointerInfoEXT importInfo = new ImportMemoryHostPointerInfoEXT()
            {
                SType = StructureType.ImportMemoryHostPointerInfoExt,
                HandleType = ExternalMemoryHandleTypeFlags.HostAllocationBitExt,
                PHostPointer = (void*)pageAlignedPointer
            };

            var memoryAllocateInfo = new MemoryAllocateInfo()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = pageAlignedSize,
                MemoryTypeIndex = (uint)memoryTypeIndex,
                PNext = &importInfo
            };

            Console.WriteLine($"{pageAlignedPointer:x16} {pageAlignedSize:x8}");

            Result result = _api.AllocateMemory(_device, memoryAllocateInfo, null, out var deviceMemory);

            if (result < Result.Success)
            {
                Console.WriteLine($"failed :(");
                return false;
            }

            var allocation = new MemoryAllocation(this, deviceMemory, pointer, offset, size);

            lock (_allocations)
            {
                _allocations.Add(new HostMemoryAllocation(allocation, pointer, size));
            }

            // Register this mapping for future use.

            return true;
        }

        public MemoryAllocation GetExistingAllocation(IntPtr pointer, ulong size)
        {
            lock (_allocations)
            {
                return _allocations.First(allocation => allocation.Pointer == pointer && allocation.Size == size).Allocation;
            }
        }

        public unsafe void Free(DeviceMemory memory, ulong offset, ulong size)
        {
            lock (_allocations)
            {
                _allocations.RemoveAll(allocation =>
                {
                    if (allocation.Allocation.Memory.Handle == memory.Handle)
                    {
                        Console.WriteLine($"freed {BitUtils.AlignDown(allocation.Pointer, Environment.SystemPageSize)}");
                        return true;
                    }

                    return false;
                });
            }

            _api.FreeMemory(_device, memory, null);
        }
    }
}
