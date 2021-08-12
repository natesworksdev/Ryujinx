using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Linq;
using VkFormat = Silk.NET.Vulkan.Format;

namespace Ryujinx.Graphics.Vulkan
{
    class FramebufferParams
    {
        private readonly Device _device;
        private readonly Auto<DisposableImageView>[] _attachments;

        public uint Width { get; }
        public uint Height { get; }
        public uint Layers { get; }

        public VkFormat[] AttachmentFormats { get; }
        public int[] AttachmentIndices { get; }

        public int AttachmentsCount { get; }
        public int MaxColorAttachmentIndex { get; }
        public bool HasDepthStencil { get; }
        public int ColorAttachmentsCount => AttachmentsCount - (HasDepthStencil ? 1 : 0);

        public FramebufferParams(
            Device device,
            Auto<DisposableImageView> view,
            uint width,
            uint height,
            bool isDepthStencil,
            VkFormat format)
        {
            _device = device;
            _attachments = new[] { view };

            Width = width;
            Height = height;
            Layers = 1;

            AttachmentFormats = new[] { format };
            AttachmentIndices = new[] { 0 };

            AttachmentsCount = 1;

            HasDepthStencil = isDepthStencil;
        }

        public FramebufferParams(Device device, ITexture[] colors, ITexture depthStencil)
        {
            _device = device;

            int colorsCount = colors.Count(IsValidTextureView);

            int count = colorsCount + (IsValidTextureView(depthStencil) ? 1 : 0);

            _attachments = new Auto<DisposableImageView>[count];

            AttachmentFormats = new VkFormat[count];
            AttachmentIndices = new int[count];
            MaxColorAttachmentIndex = colors.Length - 1;

            uint width = uint.MaxValue;
            uint height = uint.MaxValue;
            uint layers = uint.MaxValue;

            int index = 0;
            int bindIndex = 0;

            foreach (ITexture color in colors)
            {
                if (IsValidTextureView(color))
                {
                    var texture = (TextureView)color;

                    _attachments[index] = texture.GetImageViewForAttachment();

                    AttachmentFormats[index] = texture.VkFormat;
                    AttachmentIndices[index] = bindIndex;

                    width = Math.Min(width, (uint)texture.Width);
                    height = Math.Min(height, (uint)texture.Height);
                    layers = Math.Min(layers, (uint)texture.Layers);

                    if (++index >= colorsCount)
                    {
                        break;
                    }
                }

                bindIndex++;
            }

            if (depthStencil is TextureView dsTexture && dsTexture.Valid)
            {
                _attachments[count - 1] = dsTexture.GetImageViewForAttachment();

                AttachmentFormats[count - 1] = dsTexture.VkFormat;

                width = Math.Min(width, (uint)dsTexture.Width);
                height = Math.Min(height, (uint)dsTexture.Height);
                layers = Math.Min(layers, (uint)dsTexture.Layers);

                HasDepthStencil = true;
            }

            if (count == 0)
            {
                width = height = layers = 1;
            }

            Width = width;
            Height = height;
            Layers = layers;

            AttachmentsCount = count;
        }

        private static bool IsValidTextureView(ITexture texture)
        {
            return texture is TextureView view && view.Valid;
        }

        public ClearRect GetClearRect()
        {
            return new ClearRect(new Rect2D(null, new Extent2D(Width, Height)), 0, Layers);
        }

        public unsafe Auto<DisposableFramebuffer> Create(Vk api, CommandBufferScoped cbs, Auto<DisposableRenderPass> renderPass)
        {
            ImageView* attachments = stackalloc ImageView[_attachments.Length];

            for (int i = 0; i < _attachments.Length; i++)
            {
                attachments[i] = _attachments[i].Get(cbs).Value;
            }

            var framebufferCreateInfo = new FramebufferCreateInfo()
            {
                SType = StructureType.FramebufferCreateInfo,
                RenderPass = renderPass.Get(cbs).Value,
                AttachmentCount = (uint)_attachments.Length,
                PAttachments = attachments,
                Width = Width,
                Height = Height,
                Layers = Layers
            };

            api.CreateFramebuffer(_device, framebufferCreateInfo, null, out var framebuffer).ThrowOnError();
            return new Auto<DisposableFramebuffer>(new DisposableFramebuffer(api, _device, framebuffer), null, _attachments);
        }
    }
}
