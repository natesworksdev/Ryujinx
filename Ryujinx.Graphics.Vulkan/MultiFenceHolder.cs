using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Graphics.Vulkan
{
    /// <summary>
    /// Holder for multiple host GPU fences.
    /// </summary>
    class MultiFenceHolder
    {
        private readonly Dictionary<FenceHolder, int> _fences;
        private BufferRangeList _rangeList;

        /// <summary>
        /// Creates a new instance of the multiple fence holder.
        /// </summary>
        public MultiFenceHolder()
        {
            _fences = new Dictionary<FenceHolder, int>();
            _rangeList.Initialize();
        }


        public void AddBufferUse(int cbIndex, int offset, int size)
        {
            if (VulkanConfiguration.UseGranularBufferTracking)
            {
                _rangeList.Add(cbIndex, offset, size);
            }
        }

        public void RemoveBufferUses(int cbIndex)
        {
            if (VulkanConfiguration.UseGranularBufferTracking)
            {
                _rangeList.Clear(cbIndex);
            }
        }

        public bool IsBufferRangeInUse(int cbIndex, int offset, int size)
        {
            if (VulkanConfiguration.UseGranularBufferTracking)
            {
                return _rangeList.OverlapsWith(cbIndex, offset, size);
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Adds a fence to the holder.
        /// </summary>
        /// <param name="cbIndex">Command buffer index of the command buffer that owns the fence</param>
        /// <param name="fence">Fence to be added</param>
        public void AddFence(int cbIndex, FenceHolder fence)
        {
            lock (_fences)
            {
                _fences.TryAdd(fence, cbIndex);
            }
        }

        /// <summary>
        /// Removes a fence from the holder.
        /// </summary>
        /// <param name="cbIndex">Command buffer index of the command buffer that owns the fence</param>
        /// <param name="fence">Fence to be removed</param>
        public void RemoveFence(int cbIndex, FenceHolder fence)
        {
            lock (_fences)
            {
                _fences.Remove(fence);
            }
        }

        /// <summary>
        /// Wait until all the fences on the holder are signaled.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="device">GPU device that the fences belongs to</param>
        public void WaitForFences(Vk api, Device device)
        {
            WaitForFencesImpl(api, device, 0, 0, false, 0UL);
        }

        /// <summary>
        /// Wait until all the fences on the holder with buffer uses overlapping the specified range are signaled.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="device"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public void WaitForFences(Vk api, Device device, int offset, int size)
        {
            WaitForFencesImpl(api, device, offset, size, false, 0UL);
        }

        /// <summary>
        /// Wait until all the fences on the holder are signaled, or the timeout expires.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="device">GPU device that the fences belongs to</param>
        /// <param name="timeout">Timeout in nanoseconds</param>
        /// <returns>True if all fences were signaled, false otherwise</returns>
        public bool WaitForFences(Vk api, Device device, ulong timeout)
        {
            return WaitForFencesImpl(api, device, 0, 0, true, timeout);
        }

        private bool WaitForFencesImpl(Vk api, Device device, int offset, int size, bool hasTimeout, ulong timeout)
        {
            FenceHolder[] fenceHolders;
            Fence[] fences;

            lock (_fences)
            {
                fenceHolders = size != 0 && VulkanConfiguration.UseGranularBufferTracking ? GetOverlappingFences(offset, size) : _fences.Keys.ToArray();
                fences = new Fence[fenceHolders.Length];

                for (int i = 0; i < fenceHolders.Length; i++)
                {
                    fences[i] = fenceHolders[i].Get();
                }
            }

            if (fences.Length == 0)
            {
                return true;
            }

            bool signaled = true;

            if (hasTimeout)
            {
                signaled = FenceHelper.AllSignaled(api, device, fences, timeout);
            }
            else
            {
                FenceHelper.WaitAllIndefinitely(api, device, fences);
            }

            for (int i = 0; i < fenceHolders.Length; i++)
            {
                fenceHolders[i].Put();
            }

            return signaled;
        }

        public bool MayWait(Vk api, Device device, int offset, int size)
        {
            if (_fences.Count == 0)
            {
                return false;
            }

            if (VulkanConfiguration.UseGranularBufferTracking)
            {
                lock (_fences)
                {
                    foreach (var kv in _fences)
                    {
                        var fence = kv.Key;
                        var ownerCbIndex = kv.Value;

                        if (_rangeList.OverlapsWith(ownerCbIndex, offset, size))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            else
            {
                return true;
            }
        }

        private FenceHolder[] GetOverlappingFences(int offset, int size)
        {
            List<FenceHolder> overlapping = new List<FenceHolder>();

            foreach (var kv in _fences)
            {
                var fence = kv.Key;
                var ownerCbIndex = kv.Value;

                if (_rangeList.OverlapsWith(ownerCbIndex, offset, size))
                {
                    overlapping.Add(fence);
                }
            }

            return overlapping.ToArray();
        }
    }
}
