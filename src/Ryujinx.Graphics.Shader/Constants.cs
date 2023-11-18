namespace Ryujinx.Graphics.Shader
{
    static class Constants
    {
        public const int ConstantBufferSize = 0x10000; // In bytes

        public const int MaxAttributes = 16;
        public const int AllAttributesMask = (int)(uint.MaxValue >> (32 - MaxAttributes));

        public const int NvnBaseVertexByteOffset = 0x640;
        public const int NvnBaseInstanceByteOffset = 0x644;
        public const int NvnDrawIndexByteOffset = 0x648;

        public const int NvnTextureCbSlot = 2;
        public const int NvnSeparateTextureBindingsStartByteOffset = 0x168;
        public const int NvnSeparateTextureBindingsEndByteOffset = 0x568;
        public const int NvnSeparateSamplerBindingsStartByteOffset = 0x568;
        public const int NvnSeparateSamplerBindingsEndByteOffset = 0x668;

        public const int VkConstantBufferSetIndex = 0;
        public const int VkStorageBufferSetIndex = 1;
        public const int VkTextureSetIndex = 2;
        public const int VkImageSetIndex = 3;

        // Bindless emulation.

        public const int VkBindlessTextureSetIndex = 4;
        public const int VkBindlessSamplerSetIndex = 5;
        public const int VkBindlessImageSetIndex = 6;
        public const int BindlessTableBinding = 0;
        public const int BindlessScalesBinding = 1;
    }
}
