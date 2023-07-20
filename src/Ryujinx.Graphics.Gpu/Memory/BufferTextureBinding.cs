using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Gpu.Image;
using Ryujinx.Graphics.Shader;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// A buffer binding to apply to a buffer texture.
    /// </summary>
    readonly struct BufferTextureBinding
    {
        /// <summary>
        /// Shader stage accessing the texture.
        /// </summary>
        public ShaderStage Stage { get; }

        /// <summary>
        /// The buffer texture.
        /// </summary>
        public ITexture Texture { get; }

        /// <summary>
        /// The base address of the buffer binding.
        /// </summary>
        public ulong Address { get; }

        /// <summary>
        /// The size of the buffer binding in bytes.
        /// </summary>
        public ulong Size { get; }

        /// <summary>
        /// The image or sampler binding info for the buffer texture.
        /// </summary>
        public TextureBindingInfo BindingInfo { get; }

        /// <summary>
        /// The image format for the binding.
        /// </summary>
        public Format Format { get; }

        /// <summary>
        /// Whether the binding is for an image or a sampler.
        /// </summary>
        public bool IsImage { get; }

        /// <summary>
        /// Indicates if the texture should be bound as a bindless texture.
        /// </summary>
        public bool AsBindless { get; }

        /// <summary>
        /// For bindless textures, indicates the texture ID.
        /// </summary>
        public int TextureId { get; }

        /// <summary>
        /// Create a new buffer texture binding.
        /// </summary>
        /// <param name="stage">Shader stage accessing the texture</param>
        /// <param name="texture">Buffer texture</param>
        /// <param name="address">Base address</param>
        /// <param name="size">Size in bytes</param>
        /// <param name="bindingInfo">Binding info</param>
        /// <param name="format">Binding format</param>
        /// <param name="isImage">Whether the binding is for an image or a sampler</param>
        public BufferTextureBinding(
            ShaderStage stage,
            ITexture texture,
            ulong address,
            ulong size,
            TextureBindingInfo bindingInfo,
            Format format,
            bool isImage)
        {
            Stage = stage;
            Texture = texture;
            Address = address;
            Size = size;
            BindingInfo = bindingInfo;
            Format = format;
            IsImage = isImage;
            AsBindless = false;
            TextureId = 0;
        }

        /// <summary>
        /// Create a new bindless buffer texture binding.
        /// </summary>
        /// <param name="texture">Buffer texture</param>
        /// <param name="address">Base address</param>
        /// <param name="size">Size in bytes</param>
        /// <param name="bindingInfo">Binding info</param>
        /// <param name="format">Binding format</param>
        /// <param name="isImage">Whether the binding is for an image or a sampler</param>
        /// <param name="textureId">ID of the texture on the pool</param>
        public BufferTextureBinding(ITexture texture, ulong address, ulong size, TextureBindingInfo bindingInfo, Format format, bool isImage, int textureId)
        {
            Stage = default;
            Texture = texture;
            Address = address;
            Size = size;
            BindingInfo = bindingInfo;
            Format = format;
            IsImage = isImage;
            AsBindless = true;
            TextureId = textureId;
        }
    }
}
