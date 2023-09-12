using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ryujinx.Graphics.Vulkan
{
    class PipelineLayoutCacheEntry
    {
        private const uint DescriptorPoolMultiplier = 4;
        private const int MaxPoolSizesPerSet = 3;

        private readonly VulkanRenderer _gd;
        private readonly Device _device;

        public DescriptorSetLayout[] DescriptorSetLayouts { get; }
        public PipelineLayout PipelineLayout { get; }

        private readonly List<Auto<DescriptorSetCollection>>[][] _dsCache;
        private List<Auto<DescriptorSetCollection>>[] _currentDsCache;
        private readonly int[] _dsCacheCursor;
        private int _dsLastCbIndex;
        private int _dsLastSubmissionCount;

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
                    _dsCache[i][j] = new List<Auto<DescriptorSetCollection>>();
                }
            }

            _dsCacheCursor = new int[setsCount];
        }

        public PipelineLayoutCacheEntry(
            VulkanRenderer gd,
            Device device,
            ReadOnlyCollection<ResourceDescriptorCollection> setDescriptors,
            bool usePushDescriptors) : this(gd, device, setDescriptors.Count)
        {
            (DescriptorSetLayouts, PipelineLayout) = PipelineLayoutFactory.Create(gd, device, setDescriptors, usePushDescriptors);
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

                var dsc = _gd.DescriptorSetManager.AllocateDescriptorSet(_gd.Api, DescriptorSetLayouts[setIndex], poolSizes, false);
                list.Add(dsc);
                isNew = true;
                return dsc;
            }

            isNew = false;
            return list[index];
        }

        private Span<DescriptorPoolSize> GetDescriptorPoolSizes(Span<DescriptorPoolSize> output, int setIndex)
        {
            uint multiplier = DescriptorPoolMultiplier;
            int count = 1;

            switch (setIndex)
            {
                case PipelineBase.UniformSetIndex:
                    output[0] = new(DescriptorType.UniformBuffer, (1 + Constants.MaxUniformBufferBindings) * multiplier);
                    break;
                case PipelineBase.StorageSetIndex:
                    output[0] = new(DescriptorType.StorageBuffer, Constants.MaxStorageBufferBindings * multiplier);
                    break;
                case PipelineBase.TextureSetIndex:
                    output[0] = new(DescriptorType.CombinedImageSampler, Constants.MaxTextureBindings * multiplier);
                    output[1] = new(DescriptorType.UniformTexelBuffer, Constants.MaxTextureBindings * multiplier);
                    count = 2;
                    break;
                case PipelineBase.ImageSetIndex:
                    output[0] = new(DescriptorType.StorageImage, Constants.MaxImageBindings * multiplier);
                    output[1] = new(DescriptorType.StorageTexelBuffer, Constants.MaxImageBindings * multiplier);
                    count = 2;
                    break;
            }

            return output.Slice(0, count);
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
