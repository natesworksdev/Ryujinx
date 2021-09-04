namespace Ryujinx.Graphics.Vulkan
{
    static class VulkanConfiguration
    {
        public const bool UseDynamicState = true;

        public const bool UseFastBufferUpdates = true;
        public const bool UseGranularBufferTracking = false;
        public const bool UseSlowSafeBlitOnAmd = true;

        public const bool ForceD24S8Unsupported = false;
    }
}
