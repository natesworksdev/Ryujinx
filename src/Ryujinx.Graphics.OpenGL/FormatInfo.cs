using Silk.NET.OpenGL.Legacy;

namespace Ryujinx.Graphics.OpenGL
{
    readonly struct FormatInfo
    {
        public int Components { get; }
        public bool Normalized { get; }
        public bool Scaled { get; }

        public InternalFormat InternalFormat { get; }
        public PixelFormat PixelFormat { get; }
        public PixelType PixelType { get; }

        public bool IsCompressed { get; }

        public FormatInfo(
            int components,
            bool normalized,
            bool scaled,
            InternalFormat internalFormat,
            PixelFormat pixelFormat,
            PixelType pixelType)
        {
            Components = components;
            Normalized = normalized;
            Scaled = scaled;
            InternalFormat = internalFormat;
            PixelFormat = pixelFormat;
            PixelType = pixelType;
            IsCompressed = false;
        }

        public FormatInfo(int components, bool normalized, bool scaled, InternalFormat pixelFormat)
        {
            Components = components;
            Normalized = normalized;
            Scaled = scaled;
            InternalFormat = 0;
            PixelFormat = (PixelFormat)pixelFormat;
            PixelType = 0;
            IsCompressed = true;
        }
    }
}
