using Ryujinx.Graphics.Shader.Translation;

namespace Ryujinx.Graphics.GAL
{
    public struct Capabilities
    {
        public TargetApi Api { get; }

        public bool SupportsAstcCompression { get; }
        public bool SupportsImageLoadFormatted { get; }
        public bool SupportsMismatchingViewFormat { get; }
        public bool SupportsNonConstantTextureOffset { get; }
        public bool SupportsTextureShadowLod { get; }
        public bool SupportsViewportSwizzle { get; }

        public int MaximumComputeSharedMemorySize { get; }
        public float MaximumSupportedAnisotropy { get; }
        public int StorageBufferOffsetAlignment { get; }

        public Capabilities(
            TargetApi api,
            bool supportsAstcCompression,
            bool supportsImageLoadFormatted,
            bool supportsMismatchingViewFormat,
            bool supportsNonConstantTextureOffset,
            bool supportsTextureShadowLod,
            bool supportsViewportSwizzle,
            int maximumComputeSharedMemorySize,
            float maximumSupportedAnisotropy,
            int storageBufferOffsetAlignment)
        {
            Api = api;
            SupportsAstcCompression = supportsAstcCompression;
            SupportsImageLoadFormatted = supportsImageLoadFormatted;
            SupportsMismatchingViewFormat = supportsMismatchingViewFormat;
            SupportsNonConstantTextureOffset = supportsNonConstantTextureOffset;
            SupportsTextureShadowLod = supportsTextureShadowLod;
            SupportsViewportSwizzle = supportsViewportSwizzle;
            MaximumComputeSharedMemorySize = maximumComputeSharedMemorySize;
            MaximumSupportedAnisotropy = maximumSupportedAnisotropy;
            StorageBufferOffsetAlignment = storageBufferOffsetAlignment;
        }
    }
}