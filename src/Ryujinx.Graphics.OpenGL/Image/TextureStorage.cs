using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Silk.NET.OpenGL.Legacy;

namespace Ryujinx.Graphics.OpenGL.Image
{
    class TextureStorage : ITextureInfo
    {
        public ITextureInfo Storage => this;
        public uint Handle { get; private set; }

        public TextureCreateInfo Info { get; }

        private readonly OpenGLRenderer _gd;

        private int _viewsCount;

        internal ITexture DefaultView { get; private set; }

        public TextureStorage(OpenGLRenderer gd, TextureCreateInfo info)
        {
            _gd = gd;
            Info = info;

            Handle = _gd.Api.GenTexture();

            CreateImmutableStorage();
        }

        private void CreateImmutableStorage()
        {
            TextureTarget target = Info.Target.Convert();

            _gd.Api.ActiveTexture(TextureUnit.Texture0);

            _gd.Api.BindTexture(target, Handle);

            FormatInfo format = FormatTable.GetFormatInfo(Info.Format);

            SizedInternalFormat internalFormat;

            if (format.IsCompressed)
            {
                internalFormat = (SizedInternalFormat)format.PixelFormat;
            }
            else
            {
                internalFormat = (SizedInternalFormat)format.InternalFormat;
            }

            uint levels = (uint)Info.GetLevelsClamped();

            switch (Info.Target)
            {
                case Target.Texture1D:
                    _gd.Api.TexStorage1D(
                        TextureTarget.Texture1D,
                        levels,
                        internalFormat,
                        (uint)Info.Width);
                    break;

                case Target.Texture1DArray:
                    _gd.Api.TexStorage2D(
                        TextureTarget.Texture1DArray,
                        levels,
                        internalFormat,
                        (uint)Info.Width,
                        (uint)Info.Height);
                    break;

                case Target.Texture2D:
                    _gd.Api.TexStorage2D(
                        TextureTarget.Texture2D,
                        levels,
                        internalFormat,
                        (uint)Info.Width,
                        (uint)Info.Height);
                    break;

                case Target.Texture2DArray:
                    _gd.Api.TexStorage3D(
                        TextureTarget.Texture2DArray,
                        levels,
                        internalFormat,
                        (uint)Info.Width,
                        (uint)Info.Height,
                        (uint)Info.Depth);
                    break;

                case Target.Texture2DMultisample:
                    _gd.Api.TexStorage2DMultisample(
                        TextureTarget.Texture2DMultisample,
                        (uint)Info.Samples,
                        internalFormat,
                        (uint)Info.Width,
                        (uint)Info.Height,
                        true);
                    break;

                case Target.Texture2DMultisampleArray:
                    _gd.Api.TexStorage3DMultisample(
                        TextureTarget.Texture2DMultisampleArray,
                        (uint)Info.Samples,
                        internalFormat,
                        (uint)Info.Width,
                        (uint)Info.Height,
                        (uint)Info.Depth,
                        true);
                    break;

                case Target.Texture3D:
                    _gd.Api.TexStorage3D(
                        TextureTarget.Texture3D,
                        levels,
                        internalFormat,
                        (uint)Info.Width,
                        (uint)Info.Height,
                        (uint)Info.Depth);
                    break;

                case Target.Cubemap:
                    _gd.Api.TexStorage2D(
                        TextureTarget.TextureCubeMap,
                        levels,
                        internalFormat,
                        (uint)Info.Width,
                        (uint)Info.Height);
                    break;

                case Target.CubemapArray:
                    _gd.Api.TexStorage3D(
                        TextureTarget.TextureCubeMapArray,
                        levels,
                        internalFormat,
                        (uint)Info.Width,
                        (uint)Info.Height,
                        (uint)Info.Depth);
                    break;

                default:
                    Logger.Debug?.Print(LogClass.Gpu, $"Invalid or unsupported texture target: {target}.");
                    break;
            }
        }

        public ITexture CreateDefaultView()
        {
            DefaultView = CreateView(Info, 0, 0);

            return DefaultView;
        }

        public ITexture CreateView(TextureCreateInfo info, uint firstLayer, uint firstLevel)
        {
            IncrementViewsCount();

            return new TextureView(_gd, this, info, firstLayer, firstLevel);
        }

        private void IncrementViewsCount()
        {
            _viewsCount++;
        }

        public void DecrementViewsCount()
        {
            // If we don't have any views, then the storage is now useless, delete it.
            if (--_viewsCount == 0)
            {
                if (DefaultView == null)
                {
                    Dispose();
                }
                else
                {
                    // If the default view still exists, we can put it into the resource pool.
                    Release();
                }
            }
        }

        /// <summary>
        /// Release the TextureStorage to the resource pool without disposing its handle.
        /// </summary>
        public void Release()
        {
            _viewsCount = 1; // When we are used again, we will have the default view.

            _gd.ResourcePool.AddTexture((TextureView)DefaultView);
        }

        public void DeleteDefault()
        {
            DefaultView = null;
        }

        public void Dispose()
        {
            DefaultView = null;

            if (Handle != 0)
            {
                _gd.Api.DeleteTexture(Handle);

                Handle = 0;
            }
        }
    }
}
