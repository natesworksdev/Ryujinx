using Silk.NET.Vulkan;
using System;
using System.Diagnostics;

namespace Ryujinx.Graphics.Vulkan
{
    class DescriptorSetManager : IDisposable
    {
        private const uint DescriptorPoolMultiplier = 16;

        public class DescriptorPoolHolder : IDisposable
        {
            public Vk Api { get; }
            public Device Device { get; }

            private readonly DescriptorPool _pool;
            private readonly uint _capacity;
            private int _totalSets;
            private int _setsInUse;
            private bool _done;

            public unsafe DescriptorPoolHolder(Vk api, Device device)
            {
                Api = api;
                Device = device;

                var poolSizes = new DescriptorPoolSize[]
                {
                    new DescriptorPoolSize(DescriptorType.UniformBuffer, Constants.MaxUniformBufferBindings * DescriptorPoolMultiplier),
                    new DescriptorPoolSize(DescriptorType.StorageBuffer, Constants.MaxStorageBufferBindings * DescriptorPoolMultiplier),
                    new DescriptorPoolSize(DescriptorType.CombinedImageSampler, Constants.MaxTextureBindings * DescriptorPoolMultiplier)
                };

                uint maxSets = (uint)poolSizes.Length * DescriptorPoolMultiplier;

                _capacity = maxSets;

                fixed (DescriptorPoolSize* pPoolsSize = poolSizes)
                {
                    var descriptorPoolCreateInfo = new DescriptorPoolCreateInfo()
                    {
                        SType = StructureType.DescriptorPoolCreateInfo,
                        MaxSets = maxSets,
                        PoolSizeCount = (uint)poolSizes.Length,
                        PPoolSizes = pPoolsSize
                    };

                    Api.CreateDescriptorPool(device, descriptorPoolCreateInfo, null, out _pool).ThrowOnError();
                }
            }

            public unsafe DescriptorSetCollection AllocateDescriptorSets(ReadOnlySpan<DescriptorSetLayout> layouts)
            {
                Debug.Assert(!_done);
                _totalSets += layouts.Length;
                _setsInUse += layouts.Length;

                DescriptorSet[] descriptorSets = new DescriptorSet[layouts.Length];

                fixed (DescriptorSet* pDescriptorSets = descriptorSets)
                {
                    fixed (DescriptorSetLayout* pLayouts = layouts)
                    {
                        var descriptorSetAllocateInfo = new DescriptorSetAllocateInfo()
                        {
                            SType = StructureType.DescriptorSetAllocateInfo,
                            DescriptorPool = _pool,
                            DescriptorSetCount = (uint)layouts.Length,
                            PSetLayouts = pLayouts
                        };
                        Api.AllocateDescriptorSets(Device, &descriptorSetAllocateInfo, pDescriptorSets).ThrowOnError();
                    }
                }

                return new DescriptorSetCollection(this, descriptorSets);
            }

            public void FreeDescriptorSets(DescriptorSetCollection dsc)
            {
                _setsInUse -= dsc.SetsCount;
                Debug.Assert(_setsInUse >= 0);
                DestroyIfDone();
            }

            public bool CanFit(int count)
            {
                if (_totalSets + count <= _capacity)
                {
                    return true;
                }

                _done = true;
                DestroyIfDone();
                return false;
            }

            private unsafe void DestroyIfDone()
            {
                if (_done && _setsInUse == 0)
                {
                    Api.DestroyDescriptorPool(Device, _pool, null);
                }
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    unsafe
                    {
                        Api.DestroyDescriptorPool(Device, _pool, null);
                    }
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }
        }

        private readonly Device _device;
        private DescriptorPoolHolder _currentPool;

        public DescriptorSetManager(Device device)
        {
            _device = device;
        }

        public Auto<DescriptorSetCollection> AllocateDescriptorSet(Vk api, DescriptorSetLayout layout)
        {
            Span<DescriptorSetLayout> layouts = stackalloc DescriptorSetLayout[1];
            layouts[0] = layout;
            return AllocateDescriptorSets(api, layouts);
        }

        public Auto<DescriptorSetCollection> AllocateDescriptorSets(Vk api, ReadOnlySpan<DescriptorSetLayout> layouts)
        {
            return new Auto<DescriptorSetCollection>(GetPool(api, layouts.Length).AllocateDescriptorSets(layouts));
        }

        private DescriptorPoolHolder GetPool(Vk api, int requiredCount)
        {
            if (_currentPool == null || !_currentPool.CanFit(requiredCount))
            {
                _currentPool = new DescriptorPoolHolder(api, _device);
            }

            return _currentPool;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                unsafe
                {
                    _currentPool?.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
