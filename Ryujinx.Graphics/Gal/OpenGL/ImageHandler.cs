using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.Texture;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class ImageHandler
    {
        private static int CopyBuffer = 0;
        private static int CopyBufferSize = 0;

        public GalImage Image { get; private set; }

        public int Width  => Image.Width;
        public int Height => Image.Height;

        public GalImageFormat Format => Image.Format;

        public PixelInternalFormat InternalFormat { get; private set; }
        public PixelFormat         PixelFormat    { get; private set; }
        public PixelType           PixelType      { get; private set; }

        public int Handle { get; private set; }

        private bool Initialized;

        public ImageHandler()
        {
            Handle = GL.GenTexture();
        }

        public ImageHandler(int Handle, GalImage Image)
        {
            this.Handle = Handle;

            this.Image = Image;
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

            (PixelInternalFormat InternalFormat, PixelFormat PixelFormat, PixelType PixelType) =
                OGLEnumConverter.GetImageFormat(NewImage.Format);

            GL.BindTexture(TextureTarget.Texture2D, Handle);

            if (Initialized)
            {
                if (CopyBuffer == 0)
                {
                    CopyBuffer = GL.GenBuffer();
                }

                int CurrentSize = Math.Max(ImageTable.GetImageSize(NewImage),
                                           ImageTable.GetImageSize(Image));

                GL.BindBuffer(BufferTarget.PixelPackBuffer, CopyBuffer);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, CopyBuffer);

                if (CopyBufferSize < CurrentSize)
                {
                    CopyBufferSize = CurrentSize;

                    GL.BufferData(BufferTarget.PixelPackBuffer, CurrentSize, IntPtr.Zero, BufferUsageHint.StreamCopy);
                }

                GL.GetTexImage(TextureTarget.Texture2D, 0, this.PixelFormat, this.PixelType, IntPtr.Zero);

                GL.DeleteTexture(Handle);

                Handle = GL.GenTexture();

                GL.BindTexture(TextureTarget.Texture2D, Handle);
            }

            const int MinFilter = (int)TextureMinFilter.Linear;
            const int MagFilter = (int)TextureMagFilter.Linear;

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, MinFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, MagFilter);

            const int Level = 0;
            const int Border = 0;

            GL.TexImage2D(
                TextureTarget.Texture2D,
                Level,
                InternalFormat,
                NewImage.Width,
                NewImage.Height,
                Border,
                PixelFormat,
                PixelType,
                IntPtr.Zero);

            if (Initialized)
            {
                GL.BindBuffer(BufferTarget.PixelPackBuffer,   0);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
            }

            Image = NewImage;

            this.InternalFormat = InternalFormat;
            this.PixelFormat = PixelFormat;
            this.PixelType = PixelType;

            Initialized = true;
        }

        public bool HasColor   => ImageTable.HasColor(Image.Format);
        public bool HasDepth   => ImageTable.HasDepth(Image.Format);
        public bool HasStencil => ImageTable.HasStencil(Image.Format);
    }
}
