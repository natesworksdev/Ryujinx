using Ryujinx.Cpu.Tracking;
using Ryujinx.Memory.Range;
using Ryujinx.Memory.Tracking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// A tracking handle for a region of GPU VA, represented by one or more tracking handles in CPU VA.
    /// </summary>
    class GpuMultiRegionHandle : IMultiRegionHandle
    {
        private readonly CpuMultiRegionHandle[] _cpuRegionHandles;

        /// <summary>
        /// True if any write has occurred to the whole region since the last use of QueryModified (with no subregion specified).
        /// </summary>
        public bool Dirty
        {
            get
            {
                foreach (var regionHandle in _cpuRegionHandles)
                {
                    if (regionHandle.Dirty)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Create a new GpuRegionHandle, made up of mulitple CpuRegionHandles.
        /// </summary>
        /// <param name="cpuRegionHandles">The CpuRegionHandles that make up this handle</param>
        public GpuMultiRegionHandle(CpuMultiRegionHandle[] cpuRegionHandles)
        {
            _cpuRegionHandles = cpuRegionHandles;
        }

        /// <summary>
        /// Dispose the child handles.
        /// </summary>
        public void Dispose()
        {
            foreach (var regionHandle in _cpuRegionHandles)
            {
                regionHandle.Dispose();
            }
        }

        /// <summary>
        /// Force the range of handles to be dirty, without reprotecting.
        /// </summary>
        /// <param name="address">Start address of the range</param>
        /// <param name="size">Size of the range</param>
        public void ForceDirty(ulong address, ulong size)
        {
            foreach (var regionHandle in _cpuRegionHandles)
            {
                if (TryClampRange(address, size, regionHandle, out ulong clampedAddress, out ulong clampedSize))
                {
                    regionHandle.ForceDirty(clampedAddress, clampedSize);
                }
            }
        }

        public IEnumerable<IRegionHandle> GetHandles()
        {
            if (_cpuRegionHandles.Length == 1)
            {
                return _cpuRegionHandles[0].GetHandles();
            }

            return _cpuRegionHandles.SelectMany(x => x.GetHandles());
        }

        /// <summary>
        /// Check if any part of the region has been modified, and perform an action for each.
        /// Contiguous modified regions are combined.
        /// </summary>
        /// <param name="modifiedAction">Action to perform for modified regions</param>
        public void QueryModified(Action<ulong, ulong> modifiedAction)
        {
            foreach (var regionHandle in _cpuRegionHandles)
            {
                regionHandle.QueryModified(modifiedAction);
            }
        }

        /// <summary>
        /// Check if part of the region has been modified within a given range, and perform an action for each.
        /// The range is aligned to the level of granularity of the contained handles.
        /// Contiguous modified regions are combined.
        /// </summary>
        /// <param name="address">Start address of the range</param>
        /// <param name="size">Size of the range</param>
        /// <param name="modifiedAction">Action to perform for modified regions</param>
        public void QueryModified(ulong address, ulong size, Action<ulong, ulong> modifiedAction)
        {
            foreach (var regionHandle in _cpuRegionHandles)
            {
                if (TryClampRange(address, size, regionHandle, out ulong clampedAddress, out ulong clampedSize))
                {
                    regionHandle.QueryModified(clampedAddress, clampedSize, modifiedAction);
                }
            }
        }

        /// <summary>
        /// Check if part of the region has been modified within a given range, and perform an action for each.
        /// The range is aligned to the level of granularity of the contained handles.
        /// Contiguous modified regions are combined.
        /// </summary>
        /// <param name="range">Ranges of physical memory where the data is located</param>
        /// <param name="modifiedAction">Action to perform for modified regions</param>
        public void QueryModified(MultiRange range, Action<ulong, ulong> modifiedAction)
        {
            for (int index = 0; index < range.Count; index++)
            {
                MemoryRange subRange = range.GetSubRange(index);

                if (subRange.Address != MemoryManager.PteUnmapped)
                {
                    QueryModified(subRange.Address, subRange.Size, modifiedAction);
                }
            }
        }

        /// <summary>
        /// Check if part of the region has been modified within a given range, and perform an action for each.
        /// The sequence number provided is compared with each handle's saved sequence number.
        /// If it is equal, then the handle's dirty flag is ignored. Otherwise, the sequence number is saved.
        /// The range is aligned to the level of granularity of the contained handles.
        /// Contiguous modified regions are combined.
        /// </summary>
        /// <param name="address">Start address of the range</param>
        /// <param name="size">Size of the range</param>
        /// <param name="modifiedAction">Action to perform for modified regions</param>
        /// <param name="sequenceNumber">Current sequence number</param>
        public void QueryModified(ulong address, ulong size, Action<ulong, ulong> modifiedAction, int sequenceNumber)
        {
            foreach (var regionHandle in _cpuRegionHandles)
            {
                if (TryClampRange(address, size, regionHandle, out ulong clampedAddress, out ulong clampedSize))
                {
                    regionHandle.QueryModified(clampedAddress, clampedSize, modifiedAction, sequenceNumber);
                }
            }
        }

        /// <summary>
        /// Check if part of the region has been modified within a given range, and perform an action for each.
        /// The sequence number provided is compared with each handle's saved sequence number.
        /// If it is equal, then the handle's dirty flag is ignored. Otherwise, the sequence number is saved.
        /// The range is aligned to the level of granularity of the contained handles.
        /// Contiguous modified regions are combined.
        /// </summary>
        /// <param name="range">Ranges of physical memory where the data is located</param>
        /// <param name="modifiedAction">Action to perform for modified regions</param>
        /// <param name="sequenceNumber">Current sequence number</param>
        public void QueryModified(MultiRange range, Action<ulong, ulong> modifiedAction, int sequenceNumber)
        {
            for (int index = 0; index < range.Count; index++)
            {
                MemoryRange subRange = range.GetSubRange(index);

                if (subRange.Address != MemoryManager.PteUnmapped)
                {
                    QueryModified(subRange.Address, subRange.Size, modifiedAction, sequenceNumber);
                }
            }
        }

        /// <summary>
        /// Register an action to perform when the tracked region is read or written.
        /// The action is automatically removed after it runs.
        /// </summary>
        /// <param name="address">Start address of the range</param>
        /// <param name="size">Size of the range</param>
        /// <param name="action">Action to call on read or write</param>
        public void RegisterAction(ulong address, ulong size, RegionSignal action)
        {
            foreach (var regionHandle in _cpuRegionHandles)
            {
                if (TryClampRange(address, size, regionHandle, out ulong clampedAddress, out ulong clampedSize))
                {
                    regionHandle.RegisterAction(clampedAddress, clampedSize, action);
                }
            }
        }

        /// <summary>
        /// Register an action to perform when a precise access occurs (one with exact address and size).
        /// If the action returns true, read/write tracking are skipped.
        /// </summary>
        /// <param name="address">Start address of the range</param>
        /// <param name="size">Size of the range</param>
        /// <param name="action">Action to call on read or write</param>
        public void RegisterPreciseAction(ulong address, ulong size, PreciseRegionSignal action)
        {
            foreach (var regionHandle in _cpuRegionHandles)
            {
                if (TryClampRange(address, size, regionHandle, out ulong clampedAddress, out ulong clampedSize))
                {
                    regionHandle.RegisterPreciseAction(clampedAddress, clampedSize, action);
                }
            }
        }

        /// <summary>
        /// Register an action to perform when a precise access occurs (one with exact address and size).
        /// If the action returns true, read/write tracking are skipped.
        /// </summary>
        /// <param name="range">Ranges of physical memory where the data is located</param>
        /// <param name="action">Action to call on read or write</param>
        public void RegisterPreciseAction(MultiRange range, PreciseRegionSignal action)
        {
            for (int index = 0; index < range.Count; index++)
            {
                MemoryRange subRange = range.GetSubRange(index);

                if (subRange.Address != MemoryManager.PteUnmapped)
                {
                    RegisterPreciseAction(subRange.Address, subRange.Size, action);
                }
            }
        }

        /// <summary>
        /// Signal that one of the subregions of this multi-region has been modified. This sets the overall dirty flag.
        /// </summary>
        public void SignalWrite()
        {
            foreach (var regionHandle in _cpuRegionHandles)
            {
                regionHandle.SignalWrite();
            }
        }

        /// <summary>
        /// Restricts a given memory region to the region of the specified region handle.
        /// </summary>
        /// <param name="address">Start address of the range to restrict</param>
        /// <param name="size">Size of the range to restrice</param>
        /// <param name="regionHandle">Region handle that defines the range to restrict into</param>
        /// <param name="clampedAddress">Restricted address if the ranges overlap, zero otherwise</param>
        /// <param name="clampedSize">Restricted size if the ranges overlap, zero otherwise</param>
        /// <returns>True if the ranges overlap, false otherwise</returns>
        private static bool TryClampRange(ulong address, ulong size, CpuMultiRegionHandle regionHandle, out ulong clampedAddress, out ulong clampedSize)
        {
            ulong endAddress = address + size;

            if (address < regionHandle.Address + regionHandle.Size && regionHandle.Address < endAddress)
            {
                clampedAddress = Math.Max(address, regionHandle.Address);
                clampedSize = Math.Min(endAddress, regionHandle.Address + regionHandle.Size) - clampedAddress;
                return true;
            }

            clampedAddress = 0UL;
            clampedSize = 0UL;
            return false;
        }
    }
}