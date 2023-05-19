using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ryujinx.Graphics.Vulkan
{
    class PipelineLayoutCache
    {
        private readonly struct PlceKey : IEquatable<PlceKey>
        {
            public readonly ReadOnlyCollection<ResourceDescriptorCollection> SetDescriptors;
            public readonly bool UsePushDescriptors;

            public PlceKey(ReadOnlyCollection<ResourceDescriptorCollection> setDescriptors, bool usePushDescriptors)
            {
                SetDescriptors = setDescriptors;
                UsePushDescriptors = usePushDescriptors;
            }

            public override int GetHashCode()
            {
                HashCode hasher = new HashCode();

                if (SetDescriptors != null)
                {
                    foreach (var setDescriptor in SetDescriptors)
                    {
                        hasher.Add(setDescriptor);
                    }
                }

                hasher.Add(UsePushDescriptors);

                return hasher.ToHashCode();
            }

            public override bool Equals(object obj)
            {
                return obj is PlceKey other && Equals(other);
            }

            public bool Equals(PlceKey other)
            {
                if ((SetDescriptors == null) != (other.SetDescriptors == null))
                {
                    return false;
                }

                if (SetDescriptors != null)
                {
                    if (SetDescriptors.Count != other.SetDescriptors.Count)
                    {
                        return false;
                    }

                    for (int index = 0; index < SetDescriptors.Count; index++)
                    {
                        if (!SetDescriptors[index].Equals(other.SetDescriptors[index]))
                        {
                            return false;
                        }
                    }
                }

                return UsePushDescriptors == other.UsePushDescriptors;
            }
        }

        private readonly ConcurrentDictionary<PlceKey, PipelineLayoutCacheEntry> _plces;
        private readonly PipelineLayoutCacheEntry[] _plce;
        private readonly List<PipelineLayoutCacheEntry> _plceMinimal;

        public PipelineLayoutCache()
        {
            _plces = new ConcurrentDictionary<PlceKey, PipelineLayoutCacheEntry>();
            _plce = new PipelineLayoutCacheEntry[1 << Constants.MaxShaderStages];
            _plceMinimal = new List<PipelineLayoutCacheEntry>();
        }

        public PipelineLayoutCacheEntry Create(VulkanRenderer gd, Device device, ShaderSource[] shaders)
        {
            var plce = new PipelineLayoutCacheEntry(gd, device, shaders);
            _plceMinimal.Add(plce);
            return plce;
        }

        public PipelineLayoutCacheEntry GetOrCreate(VulkanRenderer gd, Device device, uint stages, bool usePd)
        {
            if (_plce[stages] == null)
            {
                _plce[stages] = new PipelineLayoutCacheEntry(gd, device, stages, usePd);
            }

            return _plce[stages];
        }

        public PipelineLayoutCacheEntry GetOrCreate(
            VulkanRenderer gd,
            Device device,
            ReadOnlyCollection<ResourceDescriptorCollection> setDescriptors,
            bool usePushDescriptors)
        {
            var key = new PlceKey(setDescriptors, usePushDescriptors);

            return _plces.GetOrAdd(key, (newKey) => new PipelineLayoutCacheEntry(gd, device, setDescriptors, usePushDescriptors));
        }

        protected virtual unsafe void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var plce in _plces.Values)
                {
                    plce.Dispose();
                }

                _plces.Clear();

                for (int i = 0; i < _plce.Length; i++)
                {
                    _plce[i]?.Dispose();
                }

                foreach (var plce in _plceMinimal)
                {
                    plce.Dispose();
                }

                _plceMinimal.Clear();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
