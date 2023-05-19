using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// Shader info structure builder.
    /// </summary>
    class ShaderInfoBuilder
    {
        private const int TotalSets = 4;

        private const int UniformSetIndex = 0;
        private const int StorageSetIndex = 1;
        private const int TextureSetIndex = 2;
        private const int ImageSetIndex = 3;

        private const ResourceStages SupportBufferStags =
            ResourceStages.Compute |
            ResourceStages.Vertex |
            ResourceStages.Fragment;

        private readonly GpuContext _context;

        private int _fragmentOutputMap;

        private readonly List<ResourceDescriptor>[] _resourceDescriptors;
        private readonly List<ResourceUsage>[] _resourceUsages;

        public ShaderInfoBuilder(GpuContext context)
        {
            _context = context;

            _fragmentOutputMap = -1;

            _resourceDescriptors = new List<ResourceDescriptor>[TotalSets];
            _resourceUsages = new List<ResourceUsage>[TotalSets];

            for (int index = 0; index < TotalSets; index++)
            {
                _resourceDescriptors[index] = new();
                _resourceUsages[index] = new();
            }

            AddDescriptor(SupportBufferStags, ResourceType.UniformBuffer, UniformSetIndex, 0, 1);
        }

        public void AddStageInfo(ShaderProgramInfo info)
        {
            if (info.Stage == ShaderStage.Fragment)
            {
                _fragmentOutputMap = info.FragmentOutputMap;
            }

            int stageIndex = GpuAccessorBase.GetStageIndex(info.Stage switch
            {
                ShaderStage.TessellationControl => 1,
                ShaderStage.TessellationEvaluation => 2,
                ShaderStage.Geometry => 3,
                ShaderStage.Fragment => 4,
                _ => 0
            });

            ResourceStages stages = info.Stage switch
            {
                ShaderStage.Compute => ResourceStages.Compute,
                ShaderStage.Vertex => ResourceStages.Vertex,
                ShaderStage.TessellationControl => ResourceStages.TessellationControl,
                ShaderStage.TessellationEvaluation => ResourceStages.TessellationEvaluation,
                ShaderStage.Geometry => ResourceStages.Geometry,
                ShaderStage.Fragment => ResourceStages.Fragment,
                _ => ResourceStages.None
            };

            int uniformsPerStage = (int)_context.Capabilities.MaximumUniformBuffersPerStage;
            int storagesPerStage = (int)_context.Capabilities.MaximumStorageBuffersPerStage;
            int texturesPerStage = (int)_context.Capabilities.MaximumTexturesPerStage;
            int imagesPerStage = (int)_context.Capabilities.MaximumImagesPerStage;

            int uniformBinding = 1 + stageIndex * uniformsPerStage;
            int storageBinding = stageIndex * storagesPerStage;
            int textureBinding = stageIndex * texturesPerStage * 2;
            int imageBinding = stageIndex * imagesPerStage * 2;

            AddDescriptor(stages, ResourceType.UniformBuffer, UniformSetIndex, uniformBinding, uniformsPerStage);
            AddArrayDescriptor(stages, ResourceType.StorageBuffer, StorageSetIndex, storageBinding, storagesPerStage);
            AddDualDescriptor(stages, ResourceType.TextureAndSampler, ResourceType.BufferTexture, TextureSetIndex, textureBinding, texturesPerStage);
            AddDualDescriptor(stages, ResourceType.Image, ResourceType.BufferImage, ImageSetIndex, imageBinding, imagesPerStage);

            AddUsage(info.CBuffers, stages, UniformSetIndex, isStorage: false);
            AddUsage(info.SBuffers, stages, StorageSetIndex, isStorage: true);
            AddUsage(info.Textures, stages, TextureSetIndex, isImage: false);
            AddUsage(info.Images, stages, ImageSetIndex, isImage: true);
        }

        private void AddDescriptor(ResourceStages stages, ResourceType type, int setIndex, int binding, int count)
        {
            for (int index = 0; index < count; index++)
            {
                _resourceDescriptors[setIndex].Add(new ResourceDescriptor(binding + index, 1, type, stages));
            }
        }

        private void AddDualDescriptor(ResourceStages stages, ResourceType type, ResourceType type2, int setIndex, int binding, int count)
        {
            AddDescriptor(stages, type, setIndex, binding, count);
            AddDescriptor(stages, type2, setIndex, binding + count, count);
        }

        private void AddArrayDescriptor(ResourceStages stages, ResourceType type, int setIndex, int binding, int count)
        {
            _resourceDescriptors[setIndex].Add(new ResourceDescriptor(binding, count, type, stages));
        }

        private void AddUsage(IEnumerable<BufferDescriptor> buffers, ResourceStages stages, int setIndex, bool isStorage)
        {
            foreach (BufferDescriptor buffer in buffers)
            {
                _resourceUsages[setIndex].Add(new ResourceUsage(
                    buffer.Binding,
                    isStorage ? ResourceType.StorageBuffer : ResourceType.UniformBuffer,
                    stages,
                    buffer.Flags.HasFlag(BufferUsageFlags.Write) ? ResourceAccess.ReadWrite : ResourceAccess.Read));
            }
        }

        private void AddUsage(IEnumerable<TextureDescriptor> textures, ResourceStages stages, int setIndex, bool isImage)
        {
            foreach (TextureDescriptor texture in textures)
            {
                bool isBuffer = (texture.Type & SamplerType.Mask) == SamplerType.TextureBuffer;

                ResourceType type = isBuffer
                    ? (isImage ? ResourceType.BufferImage : ResourceType.BufferTexture)
                    : (isImage ? ResourceType.Image : ResourceType.TextureAndSampler);

                _resourceUsages[setIndex].Add(new ResourceUsage(
                    texture.Binding,
                    type,
                    stages,
                    texture.Flags.HasFlag(TextureUsageFlags.ImageStore) ? ResourceAccess.ReadWrite : ResourceAccess.Read));
            }
        }

        public ShaderInfo Build(ProgramPipelineState? pipeline, bool fromCache = false)
        {
            var descriptors = new ResourceDescriptorCollection[TotalSets];
            var usages = new ResourceUsageCollection[TotalSets];

            for (int index = 0; index < TotalSets; index++)
            {
                descriptors[index] = new ResourceDescriptorCollection(_resourceDescriptors[index].ToArray().AsReadOnly());
                usages[index] = new ResourceUsageCollection(_resourceUsages[index].ToArray().AsReadOnly());
            }

            ResourceLayout resourceLayout = new ResourceLayout(descriptors.AsReadOnly(), usages.AsReadOnly());

            if (pipeline.HasValue)
            {
                return new ShaderInfo(_fragmentOutputMap, resourceLayout, pipeline.Value, fromCache);
            }
            else
            {
                return new ShaderInfo(_fragmentOutputMap, resourceLayout, fromCache);
            }
        }

        public static ShaderInfo BuildForCache(
            GpuContext context,
            IEnumerable<CachedShaderStage> programs,
            ProgramPipelineState? pipeline,
            bool fromCache = false)
        {
            ShaderInfoBuilder builder = new ShaderInfoBuilder(context);

            foreach (CachedShaderStage program in programs)
            {
                builder.AddStageInfo(program.Info);
            }

            return builder.Build(pipeline, fromCache);
        }

        public static ShaderInfo BuildForCompute(GpuContext context, ShaderProgramInfo info, bool fromCache = false)
        {
            ShaderInfoBuilder builder = new ShaderInfoBuilder(context);

            builder.AddStageInfo(info);

            return builder.Build(null, fromCache);
        }
    }
}