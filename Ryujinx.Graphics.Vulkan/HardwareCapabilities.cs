namespace Ryujinx.Graphics.Vulkan
{
    struct HardwareCapabilities
    {
        public bool SupportsConditionalRendering { get; }
        public bool SupportsExtendedDynamicState { get; }

        public HardwareCapabilities(
            bool supportsConditionalRendering,
            bool supportsExtendedDynamicState)
        {
            SupportsConditionalRendering = supportsConditionalRendering;
            SupportsExtendedDynamicState = supportsExtendedDynamicState;
        }
    }
}
