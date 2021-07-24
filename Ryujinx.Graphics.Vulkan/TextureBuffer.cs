using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using VkFormat = Silk.NET.Vulkan.Format;

namespace Ryujinx.Graphics.Vulkan
{
    class TextureBuffer : ITexture
    {
        private readonly VulkanGraphicsDevice _gd;

        private BufferHandle _bufferHandle;
        private int _offset;
        private int _size;
        private Auto<DisposableBufferView> _bufferView;

        public int Width { get; }
        public int Height { get; }

        public VkFormat VkFormat { get; }

        public float ScaleFactor { get; }

        public TextureBuffer(VulkanGraphicsDevice gd, TextureCreateInfo info, float scale)
        {
            _gd = gd;
            Width = info.Width;
            Height = info.Height;
            VkFormat = FormatTable.GetFormat(info.Format);
            ScaleFactor = scale;

            gd.Textures.Add(this);
        }

        public void CopyTo(ITexture destination, int firstLayer, int firstLevel)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(ITexture destination, int srcLayer, int dstLayer, int srcLevel, int dstLevel)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(ITexture destination, Extents2D srcRegion, Extents2D dstRegion, bool linearFilter)
        {
            throw new NotSupportedException();
        }

        public ITexture CreateView(TextureCreateInfo info, int firstLayer, int firstLevel)
        {
            throw new NotSupportedException();
        }

        public ReadOnlySpan<byte> GetData()
        {
            return _gd.GetBufferData(_bufferHandle, _offset, _size);
        }

        public void Release()
        {
            if (_gd.Textures.Remove(this))
            {
                ReleaseImpl();
            }
        }

        private void ReleaseImpl()
        {
            _bufferView?.Dispose();
            _bufferView = null;
        }

        public void SetData(ReadOnlySpan<byte> data)
        {
            _gd.SetBufferData(_bufferHandle, _offset, data);
        }

        public void SetData(ReadOnlySpan<byte> data, int layer, int level)
        {
            throw new NotSupportedException();
        }

        public void SetStorage(BufferRange buffer)
        {
            if (_bufferHandle == buffer.Handle &&
                _offset == buffer.Offset &&
                _size == buffer.Size)
            {
                return;
            }

            _bufferHandle = buffer.Handle;
            _offset = buffer.Offset;
            _size = buffer.Size;

            ReleaseImpl();;
        }

        public BufferView GetBufferView(CommandBufferScoped cbs)
        {
            if (_bufferView == null)
            {
                _bufferView = _gd.BufferManager.CreateView(_bufferHandle, VkFormat, _offset, _size);
            }

            return _bufferView?.Get(cbs, _offset, _size).Value ?? default;
        }
    }
}
