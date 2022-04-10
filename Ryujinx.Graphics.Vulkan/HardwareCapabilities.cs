using Silk.NET.Vulkan;

namespace Ryujinx.Graphics.Vulkan
{
    struct HardwareCapabilities
    {
        public bool SupportsConditionalRendering { get; }
        public bool SupportsExtendedDynamicState { get; }
        public uint MinSubgroupSize { get; }
        public uint MaxSubgroupSize { get; }
        public ShaderStageFlags RequiredSubgroupSizeStages { get; }

        public HardwareCapabilities(
            bool supportsConditionalRendering,
            bool supportsExtendedDynamicState,
            uint minSubgroupSize,
            uint maxSubgroupSize,
            ShaderStageFlags requiredSubgroupSizeStages)
        {
            SupportsConditionalRendering = supportsConditionalRendering;
            SupportsExtendedDynamicState = supportsExtendedDynamicState;
            MinSubgroupSize = minSubgroupSize;
            MaxSubgroupSize = maxSubgroupSize;
            RequiredSubgroupSizeStages = requiredSubgroupSizeStages;
        }
    }
}
