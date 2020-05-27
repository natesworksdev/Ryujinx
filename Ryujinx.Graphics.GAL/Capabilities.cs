namespace Ryujinx.Graphics.GAL
{
    public struct Capabilities
    {
        public bool SupportsAstcCompression          { get; }
        public bool SupportsImageLoadStoreFormatted  { get; }
        public bool SupportsNonConstantTextureOffset { get; }

        public int MaximumComputeSharedMemorySize { get; }
        public int StorageBufferOffsetAlignment   { get; }

        public float MaxSupportedAnisotropy { get; }

        public Capabilities(
            bool  supportsAstcCompression,
            bool  supportsImageLoadStoreFormatted,
            bool  supportsNonConstantTextureOffset,
            int   maximumComputeSharedMemorySize,
            int   storageBufferOffsetAlignment,
            float maxSupportedAnisotropy)
        {
            SupportsAstcCompression          = supportsAstcCompression;
            SupportsImageLoadStoreFormatted  = supportsImageLoadStoreFormatted;
            SupportsNonConstantTextureOffset = supportsNonConstantTextureOffset;
            MaximumComputeSharedMemorySize   = maximumComputeSharedMemorySize;
            StorageBufferOffsetAlignment     = storageBufferOffsetAlignment;
            MaxSupportedAnisotropy           = maxSupportedAnisotropy;
        }
    }
}