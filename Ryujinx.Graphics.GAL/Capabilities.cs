namespace Ryujinx.Graphics.GAL
{
    public struct Capabilities
    {
        public bool SupportsAstcCompression          { get; }
        public bool SupportsImageLoadFormatted       { get; }
        public bool SupportsNonConstantTextureOffset { get; }
        public bool SupportsViewportSwizzle          { get; }

        public int   MaximumComputeSharedMemorySize { get; }
        public float MaximumSupportedAnisotropy     { get; }
        public bool  ShaderMaxThreads32             { get; }
        public int   StorageBufferOffsetAlignment   { get; }

        public Capabilities(
            bool  supportsAstcCompression,
            bool  supportsImageLoadFormatted,
            bool  supportsNonConstantTextureOffset,
            bool  supportsViewportSwizzle,
            int   maximumComputeSharedMemorySize,
            float maximumSupportedAnisotropy,
            bool  shaderMaxThreads32,
            int   storageBufferOffsetAlignment)
        {
            SupportsAstcCompression          = supportsAstcCompression;
            SupportsImageLoadFormatted       = supportsImageLoadFormatted;
            SupportsNonConstantTextureOffset = supportsNonConstantTextureOffset;
            SupportsViewportSwizzle          = supportsViewportSwizzle;
            MaximumComputeSharedMemorySize   = maximumComputeSharedMemorySize;
            MaximumSupportedAnisotropy       = maximumSupportedAnisotropy;
            ShaderMaxThreads32               = shaderMaxThreads32;
            StorageBufferOffsetAlignment     = storageBufferOffsetAlignment;
        }
    }
}