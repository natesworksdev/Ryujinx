using Ryujinx.Graphics.GAL;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Gpu.Memory
{
    class BufferPageTable
    {
        private const int PageBits = MemoryManager.PtPageBits;
        private const ulong PageSize = MemoryManager.PageSize;
        private const ulong PageMask = MemoryManager.PageMask;

        private const int AsBits = 40;
        private const ulong AsSize = 1UL << AsBits;
        private const int AsPtBits = AsBits - PageBits;
        private const int AsPtLevels = 2;
        private const int AsPtLevelBits = AsPtBits / AsPtLevels;

        private const int PtLevel0Shift = PageBits;
        private const int PtLevel1Shift = PtLevel0Shift + AsPtLevelBits;
        private const ulong PtLevelMask = (1UL << AsPtLevelBits) - 1;

        private readonly GpuContext _context;

        private struct BufferMapping
        {
            public readonly ulong CpuAddress;
            public readonly ulong GpuAddress;
            public readonly ulong Size;

            public BufferMapping(ulong cpuAddress, ulong gpuAddress, ulong size)
            {
                CpuAddress = cpuAddress;
                GpuAddress = gpuAddress;
                Size = size;
            }
        }

        private BufferMapping[] _mappings;
        private BufferHandle _bufferMap;
        private ulong _bufferMapHostGpuAddress;
        private int _bufferMapSize;

        private readonly Dictionary<int, int> _blockIdMap;
        private readonly ulong[] _blockBitmap;

        private readonly int[] _idMap;
        private bool _idMapDataDirty;

        public BufferPageTable(GpuContext context)
        {
            _context = context;

            _blockIdMap = new Dictionary<int, int>();
            _blockBitmap = new ulong[((1 << AsPtLevelBits) + 63) / 64];

            _idMap = new int[1 << AsPtLevelBits];
        }

        public void Update(MemoryManager memoryManager, bool forceUpdate)
        {
            BufferCache bufferCache = memoryManager.Physical.BufferCache;

            if (memoryManager.MappingsModified || forceUpdate)
            {
                Mapping[] mappings = memoryManager.GetMappings();

                BufferMapping[] bufferMappings = new BufferMapping[mappings.Length];

                for (int i = 0; i < mappings.Length; i++)
                {
                    Mapping mapping = mappings[i];
                    ulong cpuAddress = bufferCache.TranslateAndCreateBuffer(memoryManager, mapping.Address, mapping.Size);

                    bufferMappings[i] = new BufferMapping(cpuAddress, mapping.Address, mapping.Size);
                }

                _mappings = bufferMappings;

                for (int i = 0; i < bufferMappings.Length; i++)
                {
                    BufferMapping mapping = bufferMappings[i];

                    ulong hostAddress = 0;

                    if (mapping.CpuAddress != 0)
                    {
                        hostAddress = bufferCache.GetBufferHostGpuAddress(mapping.CpuAddress, mapping.Size);
                    }

                    Map(hostAddress, mapping.GpuAddress, mapping.Size);
                }

                if (_idMapDataDirty)
                {
                    BufferHandle bufferMap = EnsureBufferMap(_idMap.Length * sizeof(int));
                    _context.Renderer.SetBufferData(bufferMap, 0, MemoryMarshal.Cast<int, byte>(_idMap));

                    _idMapDataDirty = false;
                }

                _context.Renderer.Pipeline.UpdatePageTableGpuAddress(_bufferMapHostGpuAddress);
            }
        }

        private void Map(ulong hostAddress, ulong guestAddress, ulong size)
        {
            ulong endGuestAddress = guestAddress + size;
            ulong blockSize = PageSize << AsPtLevelBits;

            while (guestAddress < endGuestAddress)
            {
                ulong nextGuestAddress = (guestAddress + blockSize) & ~(blockSize - 1);

                ulong chunckSize = Math.Min(nextGuestAddress - guestAddress, endGuestAddress - guestAddress);

                int pages = (int)(chunckSize / PageSize);

                int blockRegionOffset = sizeof(uint) << AsPtLevelBits;
                int blockOffset = GetBlockId(guestAddress) * (sizeof(ulong) << AsPtLevelBits);
                int blockInnerOffset = (int)((guestAddress >> PtLevel0Shift) & PtLevelMask) * sizeof(ulong);
                int baseOffset = blockRegionOffset + blockOffset + blockInnerOffset;

                ulong[] data = new ulong[pages];

                for (int page = 0; page < pages; page++)
                {
                    data[page] = hostAddress;

                    if (hostAddress != 0)
                    {
                        hostAddress += PageSize;
                    }
                }

                BufferHandle bufferMap = EnsureBufferMap(blockRegionOffset + blockOffset + (sizeof(ulong) << AsPtLevelBits));
                _context.Renderer.SetBufferData(bufferMap, baseOffset, MemoryMarshal.Cast<ulong, byte>(data));

                guestAddress += chunckSize;
            }
        }

        private BufferHandle EnsureBufferMap(int requiredSize)
        {
            if (requiredSize > _bufferMapSize)
            {
                BufferHandle newBuffer = _context.Renderer.CreateBuffer(requiredSize);

                if (_bufferMap != BufferHandle.Null)
                {
                    _context.Renderer.Pipeline.CopyBuffer(_bufferMap, newBuffer, 0, 0, _bufferMapSize);
                    _context.Renderer.DeleteBuffer(_bufferMap);
                }

                _bufferMap = newBuffer;
                _bufferMapHostGpuAddress = _context.Renderer.GetBufferGpuAddress(_bufferMap);
                _bufferMapSize = requiredSize;
            }

            return _bufferMap;
        }

        private int GetBlockId(ulong address)
        {
            int blockIndex = (int)((address >> PtLevel1Shift) & PtLevelMask);

            if (!_blockIdMap.TryGetValue(blockIndex, out int mappedIndex))
            {
                mappedIndex = AllocateNewBlock(_blockBitmap);

                _idMap[blockIndex] = mappedIndex << AsPtLevelBits;
                _idMapDataDirty = true;

                _blockIdMap.Add(blockIndex, mappedIndex);
            }

            return mappedIndex;
        }

        private static int AllocateNewBlock(ulong[] bitmap)
        {
            for (int index = 0; index < bitmap.Length; index++)
            {
                ref ulong v = ref bitmap[index];

                if (v == ulong.MaxValue)
                {
                    continue;
                }

                int firstFreeBit = BitOperations.TrailingZeroCount(~v);
                v |= 1UL << firstFreeBit;
                return index * 64 + firstFreeBit;
            }

            throw new InvalidOperationException("No free space left on the texture or sampler table.");
        }
    }
}