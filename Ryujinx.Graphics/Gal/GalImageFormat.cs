using System;

namespace Ryujinx.Graphics.Gal
{
    [Flags]
    public enum GalImageFormat
    {
        Astc2DStart,
        Astc2D4X4,
        Astc2D5X4,
        Astc2D5X5,
        Astc2D6X5,
        Astc2D6X6,
        Astc2D8X5,
        Astc2D8X6,
        Astc2D8X8,
        Astc2D10X5,
        Astc2D10X6,
        Astc2D10X8,
        Astc2D10X10,
        Astc2D12X10,
        Astc2D12X12,
        Astc2DEnd,

        Rgba4,
        Rgb565,
        Bgr5A1,
        Rgb5A1,
        R8,
        Rg8,
        Rgba8,
        Bgra8,
        Rgb10A2,
        R16,
        Rg16,
        Rgba16,
        R32,
        Rg32,
        Rgba32,
        R11G11B10,
        D16,
        D32,
        D24S8,
        D32S8,
        Bc1,
        Bc2,
        Bc3,
        Bc4,
        Bc5,
        BptcSfloat,
        BptcUfloat,
        BptcUnorm,

        Snorm = 1 << 26,
        Unorm = 1 << 27,
        Sint  = 1 << 28,
        Uint  = 1 << 39,
        Float = 1 << 30,
        Srgb  = 1 << 31,

        TypeMask = Snorm | Unorm | Sint | Uint | Float | Srgb,

        FormatMask = ~TypeMask
    }
}