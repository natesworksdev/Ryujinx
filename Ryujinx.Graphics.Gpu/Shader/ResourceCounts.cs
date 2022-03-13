namespace Ryujinx.Graphics.Gpu.Shader
{
    class ResourceCounts
    {
        public int UniformBuffersCount;
        public int StorageBuffersCount;
        public int TexturesCount;
        public int ImagesCount;

        public ResourceCounts()
        {
            UniformBuffersCount = 1; // The first binding is reserved for the support buffer.
        }
    }
}