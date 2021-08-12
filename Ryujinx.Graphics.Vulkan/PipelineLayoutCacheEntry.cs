using Silk.NET.Vulkan;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Vulkan
{
    class PipelineLayoutCacheEntry
    {
        private readonly VulkanGraphicsDevice _gd;
        private readonly Device _device;

        public DescriptorSetLayout[] DescriptorSetLayouts { get; }
        public PipelineLayout PipelineLayout { get; }

        private readonly List<Auto<DescriptorSetCollection>>[][] _dsCache;
        private readonly int[] _dsCacheCursor;
        private int _dsLastCbIndex;

        private readonly uint _stages;

        public PipelineLayoutCacheEntry(VulkanGraphicsDevice gd, Device device, uint stages)
        {
            _gd = gd;
            _device = device;
            _stages = stages;

            DescriptorSetLayouts = PipelineLayoutFactory.Create(gd, device, stages, out var pipelineLayout);
            PipelineLayout = pipelineLayout;

            _dsCache = new List<Auto<DescriptorSetCollection>>[CommandBufferPool.MaxCommandBuffers][];

            for (int i = 0; i < CommandBufferPool.MaxCommandBuffers; i++)
            {
                _dsCache[i] = new List<Auto<DescriptorSetCollection>>[PipelineBase.DescriptorSetLayouts];

                for (int j = 0; j < PipelineBase.DescriptorSetLayouts; j++)
                {
                    _dsCache[i][j] = new List<Auto<DescriptorSetCollection>>();
                }
            }

            _dsCacheCursor = new int[PipelineBase.DescriptorSetLayouts];
        }

        public Auto<DescriptorSetCollection> GetNewDescriptorSetCollection(
            VulkanGraphicsDevice gd,
            int commandBufferIndex,
            int setIndex,
            out bool isNew)
        {
            if (_dsLastCbIndex != commandBufferIndex)
            {
                _dsLastCbIndex = commandBufferIndex;

                for (int i = 0; i < PipelineBase.DescriptorSetLayouts; i++)
                {
                    _dsCacheCursor[i] = 0;
                }
            }

            var list = _dsCache[commandBufferIndex][setIndex];
            int index = _dsCacheCursor[setIndex]++;
            if (index == list.Count)
            {
                var dsc = gd.DescriptorSetManager.AllocateDescriptorSet(gd.Api, DescriptorSetLayouts[setIndex]);
                list.Add(dsc);
                isNew = true;
                return dsc;
            }

            isNew = false;
            return list[index];
        }

        protected virtual unsafe void Dispose(bool disposing)
        {
            if (disposing)
            {
                unsafe
                {
                    for (int i = 0; i < _dsCache.Length; i++)
                    {
                        for (int y = 0; y < _dsCache[i].Length; y++)
                        {
                            for (int z = 0; z < _dsCache[i][y].Count; z++)
                            {
                                _dsCache[i][y][z].Dispose();
                            }

                            _dsCache[i][y].Clear();
                        }
                    }

                    _gd.Api.DestroyPipelineLayout(_device, PipelineLayout, null);

                    for (int i = 0; i < DescriptorSetLayouts.Length; i++)
                    {
                        _gd.Api.DestroyDescriptorSetLayout(_device, DescriptorSetLayouts[i], null);
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
