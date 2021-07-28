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

        private readonly PipelineLayoutCacheEntry _plce;

        public PipelineLayout PipelineLayout => _plce.PipelineLayout;

        public uint Stages { get; }

        public int[][][] Bindings { get; }

        public ProgramLinkStatus LinkStatus { get; }

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

            gd.Shaders.Add(this);

            var internalShaders = new Shader[shaders.Length];

            _infos = new PipelineShaderStageCreateInfo[shaders.Length];

            LinkStatus = ProgramLinkStatus.Success;

            uint stages = 0;

            for (int i = 0; i < shaders.Length; i++)
            {
                var shader = (Shader)shaders[i];
                if (!shader.Valid)
                {
                    LinkStatus = ProgramLinkStatus.Failure;
                }

                stages |= 1u << shader.StageFlags switch
                {
                    ShaderStageFlags.ShaderStageFragmentBit => 1,
                    ShaderStageFlags.ShaderStageGeometryBit => 2,
                    ShaderStageFlags.ShaderStageTessellationControlBit => 3,
                    ShaderStageFlags.ShaderStageTessellationEvaluationBit => 4,
                    _ => 0
                };

                internalShaders[i] = shader;

                _infos[i] = internalShaders[i].GetInfo();
            }

            _plce = gd.PipelineLayoutCache.GetOrCreate(gd, device, stages);

            Stages = stages;

            int[][] GrabAll(Func<ShaderBindings, IReadOnlyCollection<int>> selector)
            {
                bool hasAny = false;
                int[][] bindings = new int[internalShaders.Length][];

                for (int i = 0; i < internalShaders.Length; i++)
                {
                    var collection = selector(internalShaders[i].Bindings);
                    hasAny |= collection.Count != 0;
                    bindings[i] = collection.ToArray();
                }

                return hasAny ? bindings : Array.Empty<int[]>();
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
        }

        public PipelineShaderStageCreateInfo[] GetInfos()
        {
            return _infos;
        }

        public ProgramLinkStatus CheckProgramLink(bool blocking)
        {
            return LinkStatus;
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

        public Auto<DescriptorSetCollection> GetNewDescriptorSetCollection(
            VulkanGraphicsDevice gd,
            int commandBufferIndex,
            int setIndex,
            out bool isNew)
        {
            return _plce.GetNewDescriptorSetCollection(gd, commandBufferIndex, setIndex, out isNew);
        }

        protected virtual unsafe void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_gd.Shaders.Remove(this))
                {
                    return;
                }

                for (int i = 0; i < _shaders.Length; i++)
                {
                    _shaders[i].Dispose();
                }

                if (_graphicsPipelineCache != null)
                {
                    foreach (Auto<DisposablePipeline> pipeline in _graphicsPipelineCache.Values)
                    {
                        pipeline.Dispose();
                    }
                }

                _computePipeline?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
