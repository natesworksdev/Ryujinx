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
        public Image.Texture Texture { get; }

        /// <summary>
        /// The buffer host texture.
        /// </summary>
        public ITexture HostTexture { get; }

        /// <summary>
        /// Buffer cache that owns the buffer.
        /// </summary>
        public BufferCache BufferCache => Texture.PhysicalMemory.BufferCache;

        /// <summary>
        /// The base address of the buffer binding.
        /// </summary>
        public ulong Address => Texture.Range.GetSubRange(0).Address;

        /// <summary>
        /// The size of the buffer binding in bytes.
        /// </summary>
        public ulong Size => Texture.Size;

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
        /// Create a new buffer texture binding.
        /// </summary>
        /// <param name="stage">Shader stage accessing the texture</param>
        /// <param name="texture">Buffer texture</param>
        /// <param name="hostTexture">Buffer host texture</param>
        /// <param name="bindingInfo">Binding info</param>
        /// <param name="format">Binding format</param>
        /// <param name="isImage">Whether the binding is for an image or a sampler</param>
        public BufferTextureBinding(
            ShaderStage stage,
            Image.Texture texture,
            ITexture hostTexture,
            TextureBindingInfo bindingInfo,
            Format format,
            bool isImage)
        {
            Stage = stage;
            Texture = texture;
            HostTexture = hostTexture;
            BindingInfo = bindingInfo;
            Format = format;
            IsImage = isImage;
        }
    }
}
