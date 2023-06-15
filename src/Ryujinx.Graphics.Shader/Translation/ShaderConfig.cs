using Ryujinx.Graphics.Shader.StructuredIr;
using System.Collections.Generic;
using System.Numerics;

namespace Ryujinx.Graphics.Shader.Translation
{
    class ShaderConfig
    {
        private const int ThreadsPerWarp = 32;

        public bool HasLayerInputAttribute { get; private set; }
        public int GpLayerInputAttribute { get; private set; }

        public IGpuAccessor GpuAccessor { get; }

        public TranslationOptions Options { get; }

        public ShaderProperties Properties => ResourceManager.Properties;

        public ResourceManager ResourceManager { get; set; }

        public int Size { get; private set; }
        public int LocalMemorySize { get; }

        public byte ClipDistancesWritten { get; private set; }

        public FeatureFlags UsedFeatures { get; private set; }

        public int Cb1DataSize { get; private set; }

        public bool LayerOutputWritten { get; private set; }
        public int LayerOutputAttribute { get; private set; }

        public ShaderDefinitions Definitions { get; }

        public AttributeUsage AttributeUsage { get; }

        public HostCapabilities HostCapabilities { get; }

        public ShaderConfig(ShaderStage stage, IGpuAccessor gpuAccessor, TranslationOptions options, int localMemorySize)
        {
            GpuAccessor = gpuAccessor;
            Options = options;
            LocalMemorySize = localMemorySize;

            if (stage == ShaderStage.Compute)
            {
                Definitions = new ShaderDefinitions(
                    ShaderStage.Compute,
                    gpuAccessor.QueryComputeLocalSizeX(),
                    gpuAccessor.QueryComputeLocalSizeY(),
                    gpuAccessor.QueryComputeLocalSizeZ());
            }
            else
            {
                Definitions = new ShaderDefinitions(stage);
            }

            AttributeUsage = new AttributeUsage(gpuAccessor);
            ResourceManager = new ResourceManager(stage, gpuAccessor);

            HostCapabilities = new HostCapabilities(
                gpuAccessor.QueryHostReducedPrecision(),
                gpuAccessor.QueryHostSupportsFragmentShaderInterlock(),
                gpuAccessor.QueryHostSupportsFragmentShaderOrderingIntel(),
                gpuAccessor.QueryHostSupportsGeometryShaderPassthrough(),
                gpuAccessor.QueryHostSupportsShaderBallot(),
                gpuAccessor.QueryHostSupportsShaderBarrierDivergence(),
                gpuAccessor.QueryHostSupportsTextureShadowLod(),
                gpuAccessor.QueryHostSupportsViewportMask());

            if (!gpuAccessor.QueryHostSupportsTransformFeedback() && gpuAccessor.QueryTransformFeedbackEnabled())
            {
                StructureType tfeInfoStruct = new(new StructureField[]
                {
                    new(AggregateType.Array | AggregateType.U32, "base_offset", 4),
                    new(AggregateType.U32, "vertex_count"),
                });

                BufferDefinition tfeInfoBuffer = new(BufferLayout.Std430, 1, Constants.TfeInfoBinding, "tfe_info", tfeInfoStruct);

                ResourceManager.Properties.AddOrUpdateStorageBuffer(Constants.TfeInfoBinding, tfeInfoBuffer);

                StructureType tfeDataStruct = new(new StructureField[]
                {
                    new(AggregateType.Array | AggregateType.U32, "data", 0),
                });

                for (int i = 0; i < Constants.TfeBuffersCount; i++)
                {
                    int binding = Constants.TfeBufferBaseBinding + i;
                    BufferDefinition tfeDataBuffer = new(BufferLayout.Std430, 1, binding, $"tfe_data{i}", tfeDataStruct);
                    ResourceManager.Properties.AddOrUpdateStorageBuffer(binding, tfeDataBuffer);
                }
            }
        }

        public ShaderConfig(
            ShaderStage stage,
            OutputTopology outputTopology,
            int maxOutputVertices,
            IGpuAccessor gpuAccessor,
            TranslationOptions options) : this(stage, gpuAccessor, options, 0)
        {
            Definitions = new ShaderDefinitions(
                stage,
                false,
                1,
                gpuAccessor.QueryPrimitiveTopology(),
                outputTopology,
                maxOutputVertices);
        }

        public ShaderConfig(
            ShaderHeader header,
            IGpuAccessor gpuAccessor,
            TranslationOptions options) : this(header.Stage, gpuAccessor, options, GetLocalMemorySize(header))
        {
            bool transformFeedbackEnabled =
                gpuAccessor.QueryTransformFeedbackEnabled() &&
                gpuAccessor.QueryHostSupportsTransformFeedback();
            TransformFeedbackOutput[] transformFeedbackOutputs = null;
            ulong transformFeedbackVecMap = 0UL;

            if (transformFeedbackEnabled)
            {
                transformFeedbackOutputs = new TransformFeedbackOutput[0xc0];

                for (int tfbIndex = 0; tfbIndex < 4; tfbIndex++)
                {
                    var locations = GpuAccessor.QueryTransformFeedbackVaryingLocations(tfbIndex);
                    var stride = GpuAccessor.QueryTransformFeedbackStride(tfbIndex);

                    for (int i = 0; i < locations.Length; i++)
                    {
                        byte wordOffset = locations[i];
                        if (wordOffset < 0xc0)
                        {
                            transformFeedbackOutputs[wordOffset] = new TransformFeedbackOutput(tfbIndex, i * 4, stride);
                            transformFeedbackVecMap |= 1UL << (wordOffset / 4);
                        }
                    }
                }
            }

            bool tessCw = false;
            TessPatchType tessPatchType = default;
            TessSpacing tessSpacing = default;

            AttributeType[] attributeTypes = null;
            AttributeType[] fragmentOutputTypes = null;

            InputTopology inputTopology = default;
            OutputTopology outputTopology = default;
            int maxOutputVertexCount = 0;

            bool dualSourceBlend = false;
            bool earlyZForce = false;

            switch (header.Stage)
            {
                case ShaderStage.Vertex:
                    attributeTypes = new AttributeType[32];

                    for (int location = 0; location < attributeTypes.Length; location++)
                    {
                        attributeTypes[location] = gpuAccessor.QueryAttributeType(location);
                    }
                    break;
                case ShaderStage.TessellationEvaluation:
                    tessCw = gpuAccessor.QueryTessCw();
                    tessPatchType = gpuAccessor.QueryTessPatchType();
                    tessSpacing = gpuAccessor.QueryTessSpacing();
                    break;
                case ShaderStage.Geometry:
                    inputTopology = gpuAccessor.QueryPrimitiveTopology();
                    outputTopology = header.OutputTopology;
                    maxOutputVertexCount = header.MaxOutputVertexCount;
                    break;
                case ShaderStage.Fragment:
                    dualSourceBlend = gpuAccessor.QueryDualSourceBlendEnable();
                    earlyZForce = gpuAccessor.QueryEarlyZForce();

                    fragmentOutputTypes = new AttributeType[8];

                    for (int location = 0; location < fragmentOutputTypes.Length; location++)
                    {
                        fragmentOutputTypes[location] = gpuAccessor.QueryFragmentOutputType(location);
                    }
                    break;
            }

            Definitions = new ShaderDefinitions(
                header.Stage,
                tessCw,
                tessPatchType,
                tessSpacing,
                header.Stage == ShaderStage.Geometry && header.GpPassthrough,
                header.ThreadsPerInputPrimitive,
                inputTopology,
                outputTopology,
                maxOutputVertexCount,
                dualSourceBlend,
                earlyZForce,
                header.ImapTypes,
                header.OmapTargets,
                header.OmapSampleMask,
                header.OmapDepth,
                options.TargetApi == TargetApi.Vulkan || gpuAccessor.QueryYNegateEnabled(),
                transformFeedbackEnabled,
                transformFeedbackVecMap,
                transformFeedbackOutputs,
                attributeTypes,
                fragmentOutputTypes);
        }

        private static int GetLocalMemorySize(ShaderHeader header)
        {
            return header.ShaderLocalMemoryLowSize + header.ShaderLocalMemoryHighSize + (header.ShaderLocalMemoryCrsSize / ThreadsPerWarp);
        }

        public AggregateType GetFragmentOutputColorType(int location)
        {
            return AggregateType.Vector4 | GpuAccessor.QueryFragmentOutputType(location).ToAggregateType();
        }

        public AggregateType GetUserDefinedType(int location, bool isOutput)
        {
            if ((!isOutput && Definitions.IaIndexing) ||
                (isOutput && Definitions.OaIndexing))
            {
                return AggregateType.Array | AggregateType.Vector4 | AggregateType.FP32;
            }

            AggregateType type = AggregateType.Vector4;

            if (Definitions.Stage == ShaderStage.Vertex && !isOutput)
            {
                type |= GpuAccessor.QueryAttributeType(location).ToAggregateType();
            }
            else
            {
                type |= AggregateType.FP32;
            }

            return type;
        }

        public int GetDepthRegister()
        {
            // The depth register is always two registers after the last color output.
            return BitOperations.PopCount((uint)Definitions.OmapTargets) + 1;
        }

        public uint ConstantBuffer1Read(int offset)
        {
            if (Cb1DataSize < offset + 4)
            {
                Cb1DataSize = offset + 4;
            }

            return GpuAccessor.ConstantBuffer1Read(offset);
        }

        public TextureFormat GetTextureFormat(int handle, int cbufSlot = -1)
        {
            // When the formatted load extension is supported, we don't need to
            // specify a format, we can just declare it without a format and the GPU will handle it.
            if (GpuAccessor.QueryHostSupportsImageLoadFormatted())
            {
                return TextureFormat.Unknown;
            }

            var format = GpuAccessor.QueryTextureFormat(handle, cbufSlot);

            if (format == TextureFormat.Unknown)
            {
                GpuAccessor.Log($"Unknown format for texture {handle}.");

                format = TextureFormat.R8G8B8A8Unorm;
            }

            return format;
        }

        private static bool FormatSupportsAtomic(TextureFormat format)
        {
            return format == TextureFormat.R32Sint || format == TextureFormat.R32Uint;
        }

        public TextureFormat GetTextureFormatAtomic(int handle, int cbufSlot = -1)
        {
            // Atomic image instructions do not support GL_EXT_shader_image_load_formatted,
            // and must have a type specified. Default to R32Sint if not available.

            var format = GpuAccessor.QueryTextureFormat(handle, cbufSlot);

            if (!FormatSupportsAtomic(format))
            {
                GpuAccessor.Log($"Unsupported format for texture {handle}: {format}.");

                format = TextureFormat.R32Sint;
            }

            return format;
        }

        public void SizeAdd(int size)
        {
            Size += size;
        }

        public void InheritFrom(ShaderConfig other)
        {
            ClipDistancesWritten |= other.ClipDistancesWritten;
            UsedFeatures |= other.UsedFeatures;

            AttributeUsage.InheritFrom(other.AttributeUsage);
        }

        public void SetLayerOutputAttribute(int attr)
        {
            LayerOutputWritten = true;
            LayerOutputAttribute = attr;
        }

        public void SetGeometryShaderLayerInputAttribute(int attr)
        {
            HasLayerInputAttribute = true;
            GpLayerInputAttribute = attr;
        }

        public void SetLastInVertexPipeline()
        {
            Definitions.LastInVertexPipeline = true;
        }

        public void MergeFromtNextStage(ShaderConfig config)
        {
            AttributeUsage.MergeFromtNextStage(Definitions.GpPassthrough, config.UsedFeatures.HasFlag(FeatureFlags.FixedFuncAttr), config.AttributeUsage);

            // We don't consider geometry shaders using the geometry shader passthrough feature
            // as being the last because when this feature is used, it can't actually modify any of the outputs,
            // so the stage that comes before it is the last one that can do modifications.
            if (config.Definitions.Stage != ShaderStage.Fragment && (config.Definitions.Stage != ShaderStage.Geometry || !config.Definitions.GpPassthrough))
            {
                Definitions.LastInVertexPipeline = false;
            }
        }

        public void MergeOutputUserAttributes(int mask, IEnumerable<int> perPatch)
        {
            AttributeUsage.MergeOutputUserAttributes(Definitions.GpPassthrough, mask, perPatch);
        }

        public void SetClipDistanceWritten(int index)
        {
            ClipDistancesWritten |= (byte)(1 << index);
        }

        public void SetUsedFeature(FeatureFlags flags)
        {
            UsedFeatures |= flags;
        }

        public ShaderProgramInfo CreateProgramInfo(ShaderIdentification identification = ShaderIdentification.None)
        {
            return new ShaderProgramInfo(
                ResourceManager.GetConstantBufferDescriptors(),
                ResourceManager.GetStorageBufferDescriptors(),
                ResourceManager.GetTextureDescriptors(),
                ResourceManager.GetImageDescriptors(),
                identification,
                GpLayerInputAttribute,
                Definitions.Stage,
                UsedFeatures.HasFlag(FeatureFlags.FragCoordXY),
                UsedFeatures.HasFlag(FeatureFlags.InstanceId),
                UsedFeatures.HasFlag(FeatureFlags.DrawParameters),
                UsedFeatures.HasFlag(FeatureFlags.RtLayer),
                ClipDistancesWritten,
                Definitions.OmapTargets);
        }
    }
}
