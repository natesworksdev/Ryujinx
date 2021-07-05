using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Graphics.Vulkan
{
    class ShaderCollection : IProgram
    {
        private readonly PipelineShaderStageCreateInfo[] _infos;
        private readonly IShader[] _shaders;

        public DescriptorSetLayout[] DescriptorSetLayouts { get; }
        public PipelineLayout PipelineLayout { get; }

        public int[][] Bindings { get; }

        public int[][][] BindingsOld { get; }

        public DescriptorSetCache DescriptorSetCache { get; }

        private readonly List<Auto<DescriptorSetCollection>>[][] _dsCache;
        private readonly int[] _dsCacheCursor;
        private int _dsLastCbIndex;

        private HashTableSlim<PipelineUid, Auto<DisposablePipeline>> _graphicsPipelineCache;
        private Auto<DisposablePipeline> _computePipeline;

        private VulkanGraphicsDevice _gd;
        private Device _device;

        public ShaderCollection(
            VulkanGraphicsDevice gd,
            Device device,
            IShader[] shaders,
            TransformFeedbackDescriptor[] transformFeedbackDescriptors)
        {
            _gd = gd;
            _device = device;
            _shaders = shaders;

            var internalShaders = new Shader[shaders.Length];

            _infos = new PipelineShaderStageCreateInfo[shaders.Length];

            for (int i = 0; i < shaders.Length; i++)
            {
                internalShaders[i] = (Shader)shaders[i];

                _infos[i] = internalShaders[i].GetInfo();
            }

            DescriptorSetLayouts = PipelineLayoutFactory.Create(_gd, _device, internalShaders, out var pipelineLayout);
            PipelineLayout = pipelineLayout;

            int[] GrabAll(Func<ShaderBindings, IReadOnlyCollection<int>> selector)
            {
                List<int> bindings = new List<int>();

                for (int i = 0; i < internalShaders.Length; i++)
                {
                    var collection = selector(internalShaders[i].Bindings);

                    bindings.AddRange(collection);
                }

                return bindings.ToArray();
            }

            Bindings = new[]
            {
                GrabAll(x => x.UniformBufferBindings),
                GrabAll(x => x.StorageBufferBindings),
                GrabAll(x => x.TextureBindings),
                GrabAll(x => x.ImageBindings),
                GrabAll(x => x.BufferTextureBindings),
                GrabAll(x => x.BufferImageBindings)
            };

            int[][] GrabAllOld(Func<ShaderBindings, IReadOnlyCollection<int>> selector)
            {
                int[][] bindings = new int[internalShaders.Length][];

                for (int i = 0; i < internalShaders.Length; i++)
                {
                    var collection = selector(internalShaders[i].Bindings);

                    bindings[i] = collection.ToArray();
                }

                return bindings;
            }

            BindingsOld = new[]
            {
                GrabAllOld(x => x.UniformBufferBindings),
                GrabAllOld(x => x.StorageBufferBindings),
                GrabAllOld(x => x.TextureBindings),
                GrabAllOld(x => x.ImageBindings),
                GrabAllOld(x => x.BufferTextureBindings),
                GrabAllOld(x => x.BufferImageBindings)
            };

            DescriptorSetCache = new DescriptorSetCache(_gd, DescriptorSetLayouts);

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

        public PipelineShaderStageCreateInfo[] GetInfos()
        {
            return _infos;
        }

        public ProgramLinkStatus CheckProgramLink(bool blocking)
        {
            return ProgramLinkStatus.Success;
        }

        protected virtual unsafe void Dispose(bool disposing)
        {
            if (disposing)
            {
                unsafe
                {
                    for (int i = 0; i < _shaders.Length; i++)
                    {
                        _shaders[i].Dispose();
                    }

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

                    if (_graphicsPipelineCache != null)
                    {
                        foreach (Auto<DisposablePipeline> pipeline in _graphicsPipelineCache.Values)
                        {
                            pipeline.Dispose();
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public byte[] GetBinary()
        {
            throw new System.NotImplementedException();
        }

        public void AddComputePipeline(Auto<DisposablePipeline> pipeline)
        {
            _computePipeline = pipeline;
        }

        public void RemoveComputePipeline()
        {
            _computePipeline = null;
        }

        public void AddGraphicsPipeline(ref PipelineUid key, Auto<DisposablePipeline> pipeline)
        {
            (_graphicsPipelineCache ??= new()).Add(ref key, pipeline);
        }

        public bool TryGetComputePipeline(out Auto<DisposablePipeline> pipeline)
        {
            pipeline = _computePipeline;
            return pipeline != null;
        }

        public bool TryGetGraphicsPipeline(ref PipelineUid key, out Auto<DisposablePipeline> pipeline)
        {
            if (_graphicsPipelineCache == null)
            {
                pipeline = default;
                return false;
            }

            return _graphicsPipelineCache.TryGetValue(ref key, out pipeline);
        }

        public bool TryRemoveGraphicsPipeline(ref PipelineUid key, out Auto<DisposablePipeline> pipeline)
        {
            if (_graphicsPipelineCache == null)
            {
                pipeline = default;
                return false;
            }

            return _graphicsPipelineCache.TryRemove(ref key, out pipeline);
        }

        public Auto<DescriptorSetCollection> GetNewDescriptorSetCollection(VulkanGraphicsDevice gd, int commandBufferIndex, int setIndex)
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
                return dsc;
            }

            return list[index];
        }
    }
}
