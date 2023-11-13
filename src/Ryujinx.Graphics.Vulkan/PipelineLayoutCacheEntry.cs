using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ryujinx.Graphics.Vulkan
{
    class PipelineLayoutCacheEntry
    {
        // Those were adjusted based on current descriptor usage and the descriptor counts usually used on pipeline layouts.
        // It might be a good idea to tweak them again if those change, or maybe find a way to calculate an optimal value dynamically.
        private const uint DefaultUniformBufferPoolCapacity = 19 * DescriptorSetManager.MaxSets;
        private const uint DefaultStorageBufferPoolCapacity = 16 * DescriptorSetManager.MaxSets;
        private const uint DefaultTexturePoolCapacity = 128 * DescriptorSetManager.MaxSets;
        private const uint DefaultImagePoolCapacity = 8 * DescriptorSetManager.MaxSets;

        private const int MaxPoolSizesPerSet = 3;

        private readonly VulkanRenderer _gd;
        private readonly Device _device;

        public DescriptorSetLayout[] DescriptorSetLayouts { get; }
        public PipelineLayout PipelineLayout { get; }

        private readonly int[] _consumedDescriptorsPerSet;

        private readonly List<Auto<DescriptorSetCollection>>[][] _dsCache;
        private List<Auto<DescriptorSetCollection>>[] _currentDsCache;
        private readonly int[] _dsCacheCursor;
        private int _dsLastCbIndex;
        private int _dsLastSubmissionCount;

        private readonly uint _bindlessTexturesCount;
        private readonly uint _bindlessSamplersCount;

        private PipelineLayoutCacheEntry(VulkanRenderer gd, Device device, int setsCount)
        {
            _gd = gd;
            _device = device;

            _dsCache = new List<Auto<DescriptorSetCollection>>[CommandBufferPool.MaxCommandBuffers][];

            for (int i = 0; i < CommandBufferPool.MaxCommandBuffers; i++)
            {
                _dsCache[i] = new List<Auto<DescriptorSetCollection>>[setsCount];

                for (int j = 0; j < _dsCache[i].Length; j++)
                {
                    _dsCache[i][j] = new();
                }
            }

            _dsCacheCursor = new int[setsCount];
        }

        public PipelineLayoutCacheEntry(
            VulkanRenderer gd,
            Device device,
            ReadOnlyCollection<ResourceDescriptorCollection> setDescriptors,
            PipelineLayoutUsageInfo usageInfo) : this(gd, device, setDescriptors.Count)
        {
            (DescriptorSetLayouts, PipelineLayout) = PipelineLayoutFactory.Create(gd, device, setDescriptors, usageInfo);

            _consumedDescriptorsPerSet = new int[setDescriptors.Count];

            for (int setIndex = 0; setIndex < setDescriptors.Count; setIndex++)
            {
                int count = 0;

                foreach (var descriptor in setDescriptors[setIndex].Descriptors)
                {
                    count += descriptor.Count;
                }

                _consumedDescriptorsPerSet[setIndex] = count;
            }

            _bindlessSamplersCount = usageInfo.BindlessSamplersCount;
            _bindlessTexturesCount = usageInfo.BindlessTexturesCount;
        }

        public void UpdateCommandBufferIndex(int commandBufferIndex)
        {
            int submissionCount = _gd.CommandBufferPool.GetSubmissionCount(commandBufferIndex);

            if (_dsLastCbIndex != commandBufferIndex || _dsLastSubmissionCount != submissionCount)
            {
                _dsLastCbIndex = commandBufferIndex;
                _dsLastSubmissionCount = submissionCount;
                Array.Clear(_dsCacheCursor);
            }

            _currentDsCache = _dsCache[commandBufferIndex];
        }

        public Auto<DescriptorSetCollection> GetNewDescriptorSetCollection(int setIndex, out bool isNew)
        {
            var list = _currentDsCache[setIndex];
            int index = _dsCacheCursor[setIndex]++;
            if (index == list.Count)
            {
                Span<DescriptorPoolSize> poolSizes = stackalloc DescriptorPoolSize[MaxPoolSizesPerSet];
                poolSizes = GetDescriptorPoolSizes(poolSizes, setIndex);

                int consumedDescriptors = _consumedDescriptorsPerSet[setIndex];

                // All bindless resources have the update after bind flag set,
                // this is required for Intel, otherwise it just crashes when binding the descriptor sets
                // for bindless resources (maybe because it is above the limit?)
                bool updateAfterBind = setIndex >= PipelineBase.BindlessTexturesSetIndex;

                var dsc = _gd.DescriptorSetManager.AllocateDescriptorSet(
                    _gd.Api,
                    DescriptorSetLayouts[setIndex],
                    poolSizes,
                    setIndex,
                    consumedDescriptors,
                    updateAfterBind);

                list.Add(dsc);
                isNew = true;
                return dsc;
            }

            isNew = false;
            return list[index];
        }

        private Span<DescriptorPoolSize> GetDescriptorPoolSizes(Span<DescriptorPoolSize> output, int setIndex)
        {
            int count = 1;

            switch (setIndex)
            {
                case PipelineBase.UniformSetIndex:
                    output[0] = new(DescriptorType.UniformBuffer, DefaultUniformBufferPoolCapacity);
                    break;
                case PipelineBase.StorageSetIndex:
                    output[0] = new(DescriptorType.StorageBuffer, DefaultStorageBufferPoolCapacity);
                    break;
                case PipelineBase.TextureSetIndex:
                    output[0] = new(DescriptorType.CombinedImageSampler, DefaultTexturePoolCapacity);
                    output[1] = new(DescriptorType.UniformTexelBuffer, DefaultTexturePoolCapacity);
                    count = 2;
                    break;
                case PipelineBase.ImageSetIndex:
                    output[0] = new(DescriptorType.StorageImage, DefaultImagePoolCapacity);
                    output[1] = new(DescriptorType.StorageTexelBuffer, DefaultImagePoolCapacity);
                    count = 2;
                    break;
                case PipelineBase.BindlessTexturesSetIndex:
                    output[0] = new(DescriptorType.UniformBuffer, 1);
                    output[1] = new(DescriptorType.StorageBuffer, 1);
                    output[2] = new(DescriptorType.SampledImage, _bindlessTexturesCount);
                    count = 3;
                    break;
                case PipelineBase.BindlessBufferTextureSetIndex:
                    output[0] = new(DescriptorType.UniformTexelBuffer, _bindlessTexturesCount);
                    break;
                case PipelineBase.BindlessSamplersSetIndex:
                    output[0] = new(DescriptorType.Sampler, _bindlessSamplersCount);
                    break;
                case PipelineBase.BindlessImagesSetIndex:
                    output[0] = new(DescriptorType.StorageImage, _bindlessTexturesCount);
                    break;
                case PipelineBase.BindlessBufferImageSetIndex:
                    output[0] = new(DescriptorType.StorageTexelBuffer, _bindlessTexturesCount);
                    break;
            }

            return output[..count];
        }

        protected virtual unsafe void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (int i = 0; i < _dsCache.Length; i++)
                {
                    for (int j = 0; j < _dsCache[i].Length; j++)
                    {
                        for (int k = 0; k < _dsCache[i][j].Count; k++)
                        {
                            _dsCache[i][j][k].Dispose();
                        }

                        _dsCache[i][j].Clear();
                    }
                }

                _gd.Api.DestroyPipelineLayout(_device, PipelineLayout, null);

                for (int i = 0; i < DescriptorSetLayouts.Length; i++)
                {
                    _gd.Api.DestroyDescriptorSetLayout(_device, DescriptorSetLayouts[i], null);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
