using OpenTK.Graphics.OpenGL;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class TCE
    {
        //TODO: Use a variable value here
        public const int MaxBpp = 16;

        public GalImage Image { get; private set; }

        public int Width { get => Image.Width; }
        public int Height { get => Image.Height; }
        public GalImageFormat Format { get => Image.Format; }

        public PixelInternalFormat InternalFormat { get; private set; }
        public PixelFormat PixelFormat { get; private set; }
        public PixelType PixelType { get; private set; }

        public int Handle { get; private set; }

        private bool Initialized;

        public TCE()
        {
            Handle = GL.GenTexture();
        }

        public TCE(int Handle, GalImage Image)
        {
            this.Handle = Handle;

            this.Image = Image;
        }

        public void EnsureSetup(GalImage Image)
        {
            if (this.Width != Image.Width ||
                this.Height != Image.Height ||
                this.Format != Image.Format ||
                !Initialized)
            {
                (PixelInternalFormat InternalFormat, PixelFormat PixelFormat, PixelType PixelType) =
                    OGLEnumConverter.GetImageFormat(Image.Format);

                int CopyBuffer = 0;

                bool ChangingFormat = Initialized && this.InternalFormat != InternalFormat;

                GL.BindTexture(TextureTarget.Texture2D, Handle);

                if (ChangingFormat)
                {
                    CopyBuffer = GL.GenBuffer();

                    GL.BindBuffer(BufferTarget.PixelPackBuffer, CopyBuffer);
                    GL.BindBuffer(BufferTarget.PixelUnpackBuffer, CopyBuffer);

                    int MaxWidth = Math.Max(Image.Width, this.Width);
                    int MaxHeight = Math.Max(Image.Height, this.Height);

                    GL.BufferData(BufferTarget.PixelPackBuffer, MaxWidth * MaxHeight * MaxBpp, IntPtr.Zero, BufferUsageHint.StaticCopy);

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
                    Image.Width,
                    Image.Height,
                    Border,
                    PixelFormat,
                    PixelType,
                    IntPtr.Zero);

                if (ChangingFormat)
                {
                    GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
                    GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);

                    GL.DeleteBuffer(CopyBuffer);
                }

                this.Image = Image;

                this.InternalFormat = InternalFormat;
                this.PixelFormat = PixelFormat;
                this.PixelType = PixelType;

                Initialized = true;
            }
        }
    }
}
