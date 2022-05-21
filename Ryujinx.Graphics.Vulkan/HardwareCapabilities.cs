using Silk.NET.Vulkan;

namespace Ryujinx.Graphics.Vulkan
{
    struct HardwareCapabilities
    {
        public bool SupportsConditionalRendering { get; }
        public bool SupportsExtendedDynamicState { get; }
        public bool SupportsTransformFeedback { get; }
        public bool SupportsTransformFeedbackQueries { get; }
        public bool SupportsGeometryShader { get; }
        public uint MinSubgroupSize { get; }
        public uint MaxSubgroupSize { get; }
        public ShaderStageFlags RequiredSubgroupSizeStages { get; }

        public HardwareCapabilities(
            bool supportsConditionalRendering,
            bool supportsExtendedDynamicState,
            bool supportsTransformFeedback,
            bool supportsTransformFeedbackQueries,
            bool supportsGeometryShader,
            uint minSubgroupSize,
            uint maxSubgroupSize,
            ShaderStageFlags requiredSubgroupSizeStages)
        {
            SupportsConditionalRendering = supportsConditionalRendering;
            SupportsExtendedDynamicState = supportsExtendedDynamicState;
            SupportsTransformFeedback = supportsTransformFeedback;
            SupportsTransformFeedbackQueries = supportsTransformFeedbackQueries;
            SupportsGeometryShader = supportsGeometryShader;
            MinSubgroupSize = minSubgroupSize;
            MaxSubgroupSize = maxSubgroupSize;
            RequiredSubgroupSizeStages = requiredSubgroupSizeStages;
        }
    }
}
