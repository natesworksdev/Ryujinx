using System.Collections.Generic;

namespace Ryujinx.Graphics.GAL
{
    public struct ShaderBindings
    {
        public IReadOnlyCollection<int> UniformBufferBindings { get; }
        public IReadOnlyCollection<int> StorageBufferBindings { get; }
        public IReadOnlyCollection<int> TextureBindings { get; }
        public IReadOnlyCollection<int> ImageBindings { get; }
        public IReadOnlyCollection<int> BufferTextureBindings { get; }
        public IReadOnlyCollection<int> BufferImageBindings { get; }

        public ShaderBindings(
            IReadOnlyCollection<int> uniformBufferBindings,
            IReadOnlyCollection<int> storageBufferBindings,
            IReadOnlyCollection<int> textureBindings,
            IReadOnlyCollection<int> imageBindings,
            IReadOnlyCollection<int> bufferTextureBindings,
            IReadOnlyCollection<int> bufferImageBindings)
        {
            UniformBufferBindings = uniformBufferBindings;
            StorageBufferBindings = storageBufferBindings;
            TextureBindings = textureBindings;
            ImageBindings = imageBindings;
            BufferTextureBindings = bufferTextureBindings;
            BufferImageBindings = bufferImageBindings;
        }
    }
}
