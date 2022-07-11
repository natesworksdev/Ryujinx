using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Gpu.Image;

namespace Ryujinx.Graphics.Gpu.Memory
{
    /// <summary>
    /// A buffer binding to apply to a buffer texture.
    /// </summary>
    struct BufferTextureBinding
    {
        /// <summary>
        /// The buffer texture.
        /// </summary>
        public ITexture Texture { get; }

        /// <summary>
        /// GPU virtual address of the buffer texture.
        /// </summary>
        public ulong GpuVa { get; }

        /// <summary>
        /// Size of the buffer texture in bytes.
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
        /// Create a new buffer texture binding.
        /// </summary>
        /// <param name="texture">Buffer texture</param>
        /// <param name="gpuVa">GPU virtual address of the buffer texture</param>
        /// <param name="size">Size of the buffer texture in bytes</param>
        /// <param name="bindingInfo">Binding info</param>
        /// <param name="format">Binding format</param>
        /// <param name="isImage">Whether the binding is for an image or a sampler</param>
        public BufferTextureBinding(ITexture texture, ulong gpuVa, ulong size, TextureBindingInfo bindingInfo, Format format, bool isImage)
        {
            Texture = texture;
            GpuVa = gpuVa;
            Size = size;
            BindingInfo = bindingInfo;
            Format = format;
            IsImage = isImage;
        }
    }
}
