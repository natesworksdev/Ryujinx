using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.Texture;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class ImageHandler
    {
        public GalImage Image { get; private set; }

        public int Width  => Image.Width;
        public int Height => Image.Height;

        public GalImageFormat Format => Image.Format;

        public int Handle { get; private set; }

        public bool HasColor   => ImageUtils.HasColor(Image.Format);
        public bool HasDepth   => ImageUtils.HasDepth(Image.Format);
        public bool HasStencil => ImageUtils.HasStencil(Image.Format);

        private bool Initialized;

        public ImageHandler()
        {
            Handle = GL.GenTexture();
        }

        public ImageHandler(int Handle, GalImage Image)
        {
            this.Handle = Handle;
            this.Image  = Image;
        }

        public void EnsureSetup(GalImage NewImage)
        {
            if (Width  == NewImage.Width  &&
                Height == NewImage.Height &&
                Format == NewImage.Format &&
                Initialized)
            {
                return;
            }

            PixelInternalFormat InternalFmt;
            PixelFormat         PixelFormat;
            PixelType           PixelType;

            if (ImageUtils.IsCompressed(NewImage.Format))
            {
                InternalFmt = (PixelInternalFormat)OGLEnumConverter.GetCompressedImageFormat(NewImage.Format);

                PixelFormat = default(PixelFormat);
                PixelType   = default(PixelType);
            }
            else
            {
                (InternalFmt, PixelFormat, PixelType) = OGLEnumConverter.GetImageFormat(NewImage.Format);
            }

            GL.BindTexture(TextureTarget.Texture2D, Handle);

            const int MinFilter = (int)TextureMinFilter.Linear;
            const int MagFilter = (int)TextureMagFilter.Linear;

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, MinFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, MagFilter);

            const int Level = 0;
            const int Border = 0;

            if (ImageUtils.IsCompressed(NewImage.Format))
            {
                GL.CompressedTexImage2D(
                    TextureTarget.Texture2D,
                    Level,
                    (InternalFormat)InternalFmt,
                    NewImage.Width,
                    NewImage.Height,
                    Border,
                    ImageUtils.GetSize(NewImage),
                    IntPtr.Zero);
            }
            else
            {
                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    Level,
                    InternalFmt,
                    NewImage.Width,
                    NewImage.Height,
                    Border,
                    PixelFormat,
                    PixelType,
                    IntPtr.Zero);
            }

            Image = NewImage;

            Initialized = true;
        }
    }
}
