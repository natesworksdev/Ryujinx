using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public ProgramLinkStatus LinkStatus { private set; get; }

        public bool IsLinked
        {
            get
            {
                if (LinkStatus == ProgramLinkStatus.Incomplete)
                {
                    CheckProgramLink(true);
                }

                return LinkStatus == ProgramLinkStatus.Success;
            }
        }

        private HashTableSlim<PipelineUid, Auto<DisposablePipeline>> _graphicsPipelineCache;
        private Auto<DisposablePipeline> _computePipeline;

        private VulkanGraphicsDevice _gd;
        private Device _device;
        private bool _initialized;
        private bool _isCompute;

        private ProgramPipelineState _state;
        private DisposableRenderPass _dummyRenderPass;
        private Task _compileTask;
        private bool _firstBackgroundUse;

        public ShaderCollection(VulkanGraphicsDevice gd, Device device, IShader[] shaders)
        {
            _gd = gd;
            _device = device;
            _shaders = shaders;

            gd.Shaders.Add(this);

            var internalShaders = new Shader[shaders.Length];

            _infos = new PipelineShaderStageCreateInfo[shaders.Length];

            LinkStatus = ProgramLinkStatus.Incomplete;

            uint stages = 0;

            for (int i = 0; i < shaders.Length; i++)
            {
                var shader = (Shader)shaders[i];

                stages |= 1u << shader.StageFlags switch
                {
                    ShaderStageFlags.ShaderStageFragmentBit => 1,
                    ShaderStageFlags.ShaderStageGeometryBit => 2,
                    ShaderStageFlags.ShaderStageTessellationControlBit => 3,
                    ShaderStageFlags.ShaderStageTessellationEvaluationBit => 4,
                    _ => 0
                };

                if (shader.StageFlags == ShaderStageFlags.ShaderStageComputeBit)
                {
                    _isCompute = true;
                }

                internalShaders[i] = shader;
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

            _compileTask = Task.CompletedTask;
            _firstBackgroundUse = false;
        }

        public ShaderCollection(
            VulkanGraphicsDevice gd,
            Device device,
            IShader[] shaders,
            ProgramPipelineState state) : this(gd, device, shaders)
        {
            _state = state;

            _compileTask = BackgroundCompilation();
            _firstBackgroundUse = true;
        }

        private async Task BackgroundCompilation()
        {
            await Task.WhenAll(_shaders.Select(shader => ((Shader)shader).CompileTask));

            if (_shaders.Any(shader => ((Shader)shader).CompileStatus == ProgramLinkStatus.Failure))
            {
                LinkStatus = ProgramLinkStatus.Failure;

                return;
            }

            try
            {
                if (_isCompute)
                {
                    CreateBackgroundComputePipeline();
                }
                else
                {
                    CreateBackgroundGraphicsPipeline();
                }
            }
            catch (VulkanException e)
            {
                Logger.Error?.PrintMsg(LogClass.Gpu, $"Background Compilation failed: {e.Message}");

                LinkStatus = ProgramLinkStatus.Failure;
            }
        }

        private void EnsureShadersReady()
        {
            if (!_initialized)
            {
                CheckProgramLink(true);

                ProgramLinkStatus resultStatus = ProgramLinkStatus.Success;

                for (int i = 0; i < _shaders.Length; i++)
                {
                    var shader = (Shader)_shaders[i];

                    if (shader.CompileStatus != ProgramLinkStatus.Success)
                    {
                        resultStatus = ProgramLinkStatus.Failure;
                    }

                    _infos[i] = shader.GetInfo();
                }

                // If the link status was already set as failure by background compilation, prefer that decision.
                if (LinkStatus != ProgramLinkStatus.Failure)
                {
                    LinkStatus = resultStatus;
                }

                _initialized = true;
            }
        }

        public PipelineShaderStageCreateInfo[] GetInfos()
        {
            EnsureShadersReady();

            return _infos;
        }

        protected unsafe DisposableRenderPass CreateDummyRenderPass()
        {
            if (_dummyRenderPass.Value.Handle != 0)
            {
                return _dummyRenderPass;
            }

            return _dummyRenderPass = _state.ToRenderPass(_gd, _device);
        }

        public void CreateBackgroundComputePipeline()
        {
            PipelineState pipeline = new PipelineState();
            pipeline.Initialize();

            pipeline.Stages[0] = ((Shader)_shaders[0]).GetInfo();
            pipeline.StagesCount = 1;

            pipeline.CreateComputePipeline(_gd.Api, _device, this, (_gd.Pipeline as PipelineBase).PipelineCache);
        }

        public void CreateBackgroundGraphicsPipeline()
        {
            // To compile shaders in the background in Vulkan, we need to create valid pipelines using the shader modules.
            // The GPU provides pipeline state via the GAL that can be converted into our internal Vulkan pipeline state.
            // This should match the pipeline state at the time of the first draw. If it doesn't, then it'll likely be
            // close enough that the GPU driver will reuse the compiled shader for the different state.

            // First, we need to create a render pass object compatible with the one that will be used at runtime.
            // The active attachment formats have been provided by the abstraction layer.
            var renderPass = CreateDummyRenderPass();

            PipelineState pipeline = _state.ToVulkanPipelineState(_gd);

            // Copy the shader stage info to the pipeline.
            var stages = pipeline.Stages.ToSpan();

            for (int i = 0; i < _shaders.Length; i++)
            {
                stages[i] = ((Shader)_shaders[i]).GetInfo();
            }

            pipeline.StagesCount = (uint)_shaders.Length;
            pipeline.PipelineLayout = PipelineLayout;

            pipeline.CreateGraphicsPipeline(_gd, _device, this, (_gd.Pipeline as PipelineBase).PipelineCache, renderPass.Value);
        }

        public ProgramLinkStatus CheckProgramLink(bool blocking)
        {
            if (LinkStatus == ProgramLinkStatus.Incomplete)
            {
                ProgramLinkStatus resultStatus = ProgramLinkStatus.Success;

                foreach (Shader shader in _shaders)
                {
                    if (shader.CompileStatus == ProgramLinkStatus.Incomplete)
                    {
                        if (blocking)
                        {
                            // Wait for this shader to finish compiling.
                            shader.WaitForCompile();

                            if (shader.CompileStatus != ProgramLinkStatus.Success)
                            {
                                resultStatus = ProgramLinkStatus.Failure;
                            }
                        }
                        else
                        {
                            return ProgramLinkStatus.Incomplete;
                        }
                    }
                }

                if (!_compileTask.IsCompleted)
                {
                    if (blocking)
                    {
                        _compileTask.Wait();

                        if (LinkStatus == ProgramLinkStatus.Failure)
                        {
                            return ProgramLinkStatus.Failure;
                        }
                    }
                    else
                    {
                        return ProgramLinkStatus.Incomplete;
                    }
                }

                return resultStatus;
            }

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

            if (!_graphicsPipelineCache.TryGetValue(ref key, out pipeline))
            {
                if (_firstBackgroundUse)
                {
                    Logger.Warning?.Print(LogClass.Gpu, "Background pipeline compile missed on draw - incorrect pipeline state?");
                    _firstBackgroundUse = false;
                }

                return false;
            }

            _firstBackgroundUse = false;

            return true;
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
                if (_dummyRenderPass.Value.Handle != 0)
                {
                    _dummyRenderPass.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
