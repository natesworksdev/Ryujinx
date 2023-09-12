using Silk.NET.Vulkan;
using System;
using System.Diagnostics;

namespace Ryujinx.Graphics.Vulkan
{
    class DescriptorSetManager : IDisposable
    {
        private const uint MaxSets = 16;

        public class DescriptorPoolHolder : IDisposable
        {
            public Vk Api { get; }
            public Device Device { get; }

            private readonly DescriptorPool _pool;
            private int _totalSets;
            private int _setsInUse;
            private bool _done;

            public unsafe DescriptorPoolHolder(Vk api, Device device, ReadOnlySpan<DescriptorPoolSize> poolSizes, bool updateAfterBind)
            {
                Api = api;
                Device = device;

                fixed (DescriptorPoolSize* pPoolsSize = poolSizes)
                {
                    var descriptorPoolCreateInfo = new DescriptorPoolCreateInfo
                    {
                        SType = StructureType.DescriptorPoolCreateInfo,
                        Flags = updateAfterBind ? DescriptorPoolCreateFlags.UpdateAfterBindBit : DescriptorPoolCreateFlags.None,
                        MaxSets = MaxSets,
                        PoolSizeCount = (uint)poolSizes.Length,
                        PPoolSizes = pPoolsSize,
                    };

                    Api.CreateDescriptorPool(device, descriptorPoolCreateInfo, null, out _pool).ThrowOnError();
                }
            }

            public unsafe DescriptorSetCollection AllocateDescriptorSets(
                ReadOnlySpan<DescriptorSetLayout> layouts,
                ReadOnlySpan<DescriptorPoolSize> poolSizes)
            {
                TryAllocateDescriptorSets(layouts, poolSizes, isTry: false, out var dsc);
                return dsc;
            }

            public bool TryAllocateDescriptorSets(
                ReadOnlySpan<DescriptorSetLayout> layouts,
                ReadOnlySpan<DescriptorPoolSize> poolSizes,
                out DescriptorSetCollection dsc)
            {
                return TryAllocateDescriptorSets(layouts, poolSizes, isTry: true, out dsc);
            }

            private unsafe bool TryAllocateDescriptorSets(
                ReadOnlySpan<DescriptorSetLayout> layouts,
                ReadOnlySpan<DescriptorPoolSize> poolSize,
                bool isTry,
                out DescriptorSetCollection dsc)
            {
                Debug.Assert(!_done);

                DescriptorSet[] descriptorSets = new DescriptorSet[layouts.Length];

                fixed (DescriptorSet* pDescriptorSets = descriptorSets)
                {
                    fixed (DescriptorSetLayout* pLayouts = layouts)
                    {
                        var descriptorSetAllocateInfo = new DescriptorSetAllocateInfo
                        {
                            SType = StructureType.DescriptorSetAllocateInfo,
                            DescriptorPool = _pool,
                            DescriptorSetCount = (uint)layouts.Length,
                            PSetLayouts = pLayouts,
                        };

                        var result = Api.AllocateDescriptorSets(Device, &descriptorSetAllocateInfo, pDescriptorSets);
                        if (isTry && result == Result.ErrorOutOfPoolMemory)
                        {
                            _totalSets = (int)MaxSets;
                            _done = true;
                            DestroyIfDone();
                            dsc = default;
                            return false;
                        }

                        result.ThrowOnError();
                    }
                }

                _totalSets += layouts.Length;
                _setsInUse += layouts.Length;

                dsc = new DescriptorSetCollection(this, descriptorSets);
                return true;
            }

            public void FreeDescriptorSets(DescriptorSetCollection dsc)
            {
                _setsInUse -= dsc.SetsCount;
                Debug.Assert(_setsInUse >= 0);
                DestroyIfDone();
            }

            public bool CanFit(int count)
            {
                if (_totalSets + count <= MaxSets)
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
                GC.SuppressFinalize(this);
                Dispose(true);
            }
        }

        private readonly Device _device;
        private DescriptorPoolHolder _currentPool;

        public DescriptorSetManager(Device device)
        {
            _device = device;
        }

        public Auto<DescriptorSetCollection> AllocateDescriptorSet(
            Vk api,
            DescriptorSetLayout layout,
            ReadOnlySpan<DescriptorPoolSize> poolSizes,
            bool updateAfterBind)
        {
            Span<DescriptorSetLayout> layouts = stackalloc DescriptorSetLayout[1];
            layouts[0] = layout;
            return AllocateDescriptorSets(api, layouts, poolSizes, updateAfterBind);
        }

        public Auto<DescriptorSetCollection> AllocateDescriptorSets(
            Vk api,
            ReadOnlySpan<DescriptorSetLayout> layouts,
            ReadOnlySpan<DescriptorPoolSize> poolSizes,
            bool updateAfterBind)
        {
            // If we fail the first time, just create a new pool and try again.
            if (!GetPool(api, poolSizes, updateAfterBind, layouts.Length).TryAllocateDescriptorSets(layouts, poolSizes, out var dsc))
            {
                dsc = GetPool(api, poolSizes, updateAfterBind, layouts.Length).AllocateDescriptorSets(layouts, poolSizes);
            }

            return new Auto<DescriptorSetCollection>(dsc);
        }

        private DescriptorPoolHolder GetPool(Vk api, ReadOnlySpan<DescriptorPoolSize> poolSizes, bool updateAfterBind, int requiredCount)
        {
            if (_currentPool == null || !_currentPool.CanFit(requiredCount))
            {
                _currentPool = new DescriptorPoolHolder(api, _device, poolSizes, updateAfterBind);
            }

            return _currentPool;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _currentPool?.Dispose();
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }
    }
}
