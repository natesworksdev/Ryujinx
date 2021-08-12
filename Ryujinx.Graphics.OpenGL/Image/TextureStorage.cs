using OpenTK.Graphics.OpenGL;
using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.OpenGL.Image
{
    class TextureStorage : ITextureInfo
    {
        public ITextureInfo Storage => this;
        public int Handle { get; private set; }
        public float ScaleFactor { get; private set; }

        public TextureCreateInfo Info { get; }

        private readonly Renderer _renderer;

        private int _viewsCount;

        internal ITexture DefaultView { get; private set; }

        public TextureStorage(Renderer renderer, TextureCreateInfo info, float scaleFactor)
        {
            _renderer = renderer;
            Info      = info;

            unsafe
            {
                int localHandle = 0;

                GL.CreateTextures(info.Target.Convert(), 1, &localHandle);

                Handle = localHandle;
            }

            ScaleFactor = scaleFactor;

            CreateImmutableStorage();
        }

        private void CreateImmutableStorage()
        {
            TextureTarget target = Info.Target.Convert();

            FormatInfo format = FormatTable.GetFormatInfo(Info.Format);

            SizedInternalFormat internalFormat;

            if (format.IsCompressed)
            {
                internalFormat = (SizedInternalFormat)format.PixelFormat;
            }
            else
            {
                internalFormat = (SizedInternalFormat)format.PixelInternalFormat;
            }

            switch (Info.Target)
            {
                case Target.Texture1D:
                    GL.TextureStorage1D(
                        Handle,
                        Info.Levels,
                        internalFormat,
                        Info.Width);
                    break;

                case Target.Texture1DArray:
                    GL.TextureStorage2D(
                        Handle,
                        Info.Levels,
                        internalFormat,
                        Info.Width,
                        Info.Height);
                    break;

                case Target.Texture2D:
                    GL.TextureStorage2D(
                        Handle,
                        Info.Levels,
                        internalFormat,
                        Info.Width,
                        Info.Height);
                    break;

                case Target.Texture2DArray:
                    GL.TextureStorage3D(
                        Handle,
                        Info.Levels,
                        internalFormat,
                        Info.Width,
                        Info.Height,
                        Info.Depth);
                    break;

                case Target.Texture2DMultisample:
                    GL.TextureStorage2DMultisample(
                        Handle,
                        Info.Samples,
                        internalFormat,
                        Info.Width,
                        Info.Height,
                        true);
                    break;

                case Target.Texture2DMultisampleArray:
                    GL.TextureStorage3DMultisample(
                        Handle,
                        Info.Samples,
                        internalFormat,
                        Info.Width,
                        Info.Height,
                        Info.Depth,
                        true);
                    break;

                case Target.Texture3D:
                    GL.TextureStorage3D(
                        Handle,
                        Info.Levels,
                        internalFormat,
                        Info.Width,
                        Info.Height,
                        Info.Depth);
                    break;

                case Target.Cubemap:
                    GL.TextureStorage2D(
                        Handle,
                        Info.Levels,
                        internalFormat,
                        Info.Width,
                        Info.Height);
                    break;

                case Target.CubemapArray:
                    GL.TextureStorage3D(
                        Handle,
                        Info.Levels,
                        internalFormat,
                        Info.Width,
                        Info.Height,
                        Info.Depth);
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

        public ITexture CreateView(TextureCreateInfo info, int firstLayer, int firstLevel)
        {
            IncrementViewsCount();

            return new TextureView(_renderer, this, info, firstLayer, firstLevel);
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

            _renderer.ResourcePool.AddTexture((TextureView)DefaultView);
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
                GL.DeleteTexture(Handle);

                Handle = 0;
            }
        }
    }
}
