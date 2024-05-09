using Silk.NET.OpenGL.Legacy;
using Ryujinx.Graphics.GAL;
using System;

namespace Ryujinx.Graphics.OpenGL
{
    readonly struct FormatTable
    {
        private static readonly FormatInfo[] _table;
        private static readonly SizedInternalFormat[] _tableImage;

        static FormatTable()
        {
            int tableSize = Enum.GetNames<Format>().Length;

            _table = new FormatInfo[tableSize];
            _tableImage = new SizedInternalFormat[tableSize];

#pragma warning disable IDE0055 // Disable formatting
            Add(Format.R8Unorm,             new FormatInfo(1, true,  false, InternalFormat.R8,                PixelFormat.Red,            PixelType.UnsignedByte));
            Add(Format.R8Snorm,             new FormatInfo(1, true,  false, InternalFormat.R8SNorm,           PixelFormat.Red,            PixelType.Byte));
            Add(Format.R8Uint,              new FormatInfo(1, false, false, InternalFormat.R8ui,              PixelFormat.RedInteger,     PixelType.UnsignedByte));
            Add(Format.R8Sint,              new FormatInfo(1, false, false, InternalFormat.R8i,               PixelFormat.RedInteger,     PixelType.Byte));
            Add(Format.R16Float,            new FormatInfo(1, false, false, InternalFormat.R16f,              PixelFormat.Red,            PixelType.HalfFloat));
            Add(Format.R16Unorm,            new FormatInfo(1, true,  false, InternalFormat.R16,               PixelFormat.Red,            PixelType.UnsignedShort));
            Add(Format.R16Snorm,            new FormatInfo(1, true,  false, InternalFormat.R16SNorm,          PixelFormat.Red,            PixelType.Short));
            Add(Format.R16Uint,             new FormatInfo(1, false, false, InternalFormat.R16ui,             PixelFormat.RedInteger,     PixelType.UnsignedShort));
            Add(Format.R16Sint,             new FormatInfo(1, false, false, InternalFormat.R16i,              PixelFormat.RedInteger,     PixelType.Short));
            Add(Format.R32Float,            new FormatInfo(1, false, false, InternalFormat.R32f,              PixelFormat.Red,            PixelType.Float));
            Add(Format.R32Uint,             new FormatInfo(1, false, false, InternalFormat.R32ui,             PixelFormat.RedInteger,     PixelType.UnsignedInt));
            Add(Format.R32Sint,             new FormatInfo(1, false, false, InternalFormat.R32i,              PixelFormat.RedInteger,     PixelType.Int));
            Add(Format.R8G8Unorm,           new FormatInfo(2, true,  false, InternalFormat.RG8,               PixelFormat.RG,             PixelType.UnsignedByte));
            Add(Format.R8G8Snorm,           new FormatInfo(2, true,  false, InternalFormat.RG8SNorm,          PixelFormat.RG,             PixelType.Byte));
            Add(Format.R8G8Uint,            new FormatInfo(2, false, false, InternalFormat.RG8ui,             PixelFormat.RGInteger,      PixelType.UnsignedByte));
            Add(Format.R8G8Sint,            new FormatInfo(2, false, false, InternalFormat.RG8i,              PixelFormat.RGInteger,      PixelType.Byte));
            Add(Format.R16G16Float,         new FormatInfo(2, false, false, InternalFormat.RG16f,             PixelFormat.RG,             PixelType.HalfFloat));
            Add(Format.R16G16Unorm,         new FormatInfo(2, true,  false, InternalFormat.RG16,              PixelFormat.RG,             PixelType.UnsignedShort));
            Add(Format.R16G16Snorm,         new FormatInfo(2, true,  false, InternalFormat.RG16SNorm,         PixelFormat.RG,             PixelType.Short));
            Add(Format.R16G16Uint,          new FormatInfo(2, false, false, InternalFormat.RG16ui,            PixelFormat.RGInteger,      PixelType.UnsignedShort));
            Add(Format.R16G16Sint,          new FormatInfo(2, false, false, InternalFormat.RG16i,             PixelFormat.RGInteger,      PixelType.Short));
            Add(Format.R32G32Float,         new FormatInfo(2, false, false, InternalFormat.RG32f,             PixelFormat.RG,             PixelType.Float));
            Add(Format.R32G32Uint,          new FormatInfo(2, false, false, InternalFormat.RG32ui,            PixelFormat.RGInteger,      PixelType.UnsignedInt));
            Add(Format.R32G32Sint,          new FormatInfo(2, false, false, InternalFormat.RG32i,             PixelFormat.RGInteger,      PixelType.Int));
            Add(Format.R8G8B8Unorm,         new FormatInfo(3, true,  false, InternalFormat.Rgb8,              PixelFormat.Rgb,            PixelType.UnsignedByte));
            Add(Format.R8G8B8Snorm,         new FormatInfo(3, true,  false, InternalFormat.Rgb8SNorm,         PixelFormat.Rgb,            PixelType.Byte));
            Add(Format.R8G8B8Uint,          new FormatInfo(3, false, false, InternalFormat.Rgb8ui,            PixelFormat.RgbInteger,     PixelType.UnsignedByte));
            Add(Format.R8G8B8Sint,          new FormatInfo(3, false, false, InternalFormat.Rgb8i,             PixelFormat.RgbInteger,     PixelType.Byte));
            Add(Format.R16G16B16Float,      new FormatInfo(3, false, false, InternalFormat.Rgb16f,            PixelFormat.Rgb,            PixelType.HalfFloat));
            Add(Format.R16G16B16Unorm,      new FormatInfo(3, true,  false, InternalFormat.Rgb16,             PixelFormat.Rgb,            PixelType.UnsignedShort));
            Add(Format.R16G16B16Snorm,      new FormatInfo(3, true,  false, InternalFormat.Rgb16SNorm,        PixelFormat.Rgb,            PixelType.Short));
            Add(Format.R16G16B16Uint,       new FormatInfo(3, false, false, InternalFormat.Rgb16ui,           PixelFormat.RgbInteger,     PixelType.UnsignedShort));
            Add(Format.R16G16B16Sint,       new FormatInfo(3, false, false, InternalFormat.Rgb16i,            PixelFormat.RgbInteger,     PixelType.Short));
            Add(Format.R32G32B32Float,      new FormatInfo(3, false, false, InternalFormat.Rgb32f,            PixelFormat.Rgb,            PixelType.Float));
            Add(Format.R32G32B32Uint,       new FormatInfo(3, false, false, InternalFormat.Rgb32ui,           PixelFormat.RgbInteger,     PixelType.UnsignedInt));
            Add(Format.R32G32B32Sint,       new FormatInfo(3, false, false, InternalFormat.Rgb32i,            PixelFormat.RgbInteger,     PixelType.Int));
            Add(Format.R8G8B8A8Unorm,       new FormatInfo(4, true,  false, InternalFormat.Rgba8,             PixelFormat.Rgba,           PixelType.UnsignedByte));
            Add(Format.R8G8B8A8Snorm,       new FormatInfo(4, true,  false, InternalFormat.Rgba8SNorm,        PixelFormat.Rgba,           PixelType.Byte));
            Add(Format.R8G8B8A8Uint,        new FormatInfo(4, false, false, InternalFormat.Rgba8ui,           PixelFormat.RgbaInteger,    PixelType.UnsignedByte));
            Add(Format.R8G8B8A8Sint,        new FormatInfo(4, false, false, InternalFormat.Rgba8i,            PixelFormat.RgbaInteger,    PixelType.Byte));
            Add(Format.R16G16B16A16Float,   new FormatInfo(4, false, false, InternalFormat.Rgba16f,           PixelFormat.Rgba,           PixelType.HalfFloat));
            Add(Format.R16G16B16A16Unorm,   new FormatInfo(4, true,  false, InternalFormat.Rgba16,            PixelFormat.Rgba,           PixelType.UnsignedShort));
            Add(Format.R16G16B16A16Snorm,   new FormatInfo(4, true,  false, InternalFormat.Rgba16SNorm,       PixelFormat.Rgba,           PixelType.Short));
            Add(Format.R16G16B16A16Uint,    new FormatInfo(4, false, false, InternalFormat.Rgba16ui,          PixelFormat.RgbaInteger,    PixelType.UnsignedShort));
            Add(Format.R16G16B16A16Sint,    new FormatInfo(4, false, false, InternalFormat.Rgba16i,           PixelFormat.RgbaInteger,    PixelType.Short));
            Add(Format.R32G32B32A32Float,   new FormatInfo(4, false, false, InternalFormat.Rgba32f,           PixelFormat.Rgba,           PixelType.Float));
            Add(Format.R32G32B32A32Uint,    new FormatInfo(4, false, false, InternalFormat.Rgba32ui,          PixelFormat.RgbaInteger,    PixelType.UnsignedInt));
            Add(Format.R32G32B32A32Sint,    new FormatInfo(4, false, false, InternalFormat.Rgba32i,           PixelFormat.RgbaInteger,    PixelType.Int));
            Add(Format.S8Uint,              new FormatInfo(1, false, false, InternalFormat.StencilIndex8,     PixelFormat.StencilIndex,   PixelType.UnsignedByte));
            Add(Format.D16Unorm,            new FormatInfo(1, false, false, InternalFormat.DepthComponent16,  PixelFormat.DepthComponent, PixelType.UnsignedShort));
            Add(Format.S8UintD24Unorm,      new FormatInfo(1, false, false, InternalFormat.Depth24Stencil8,   PixelFormat.DepthStencil,   PixelType.UnsignedInt248));
            Add(Format.X8UintD24Unorm,      new FormatInfo(1, false, false, InternalFormat.DepthComponent24,  PixelFormat.DepthComponent, PixelType.UnsignedInt));
            Add(Format.D32Float,            new FormatInfo(1, false, false, InternalFormat.DepthComponent32f, PixelFormat.DepthComponent, PixelType.Float));
            Add(Format.D24UnormS8Uint,      new FormatInfo(1, false, false, InternalFormat.Depth24Stencil8,   PixelFormat.DepthStencil,   PixelType.UnsignedInt248));
            Add(Format.D32FloatS8Uint,      new FormatInfo(1, false, false, InternalFormat.Depth32fStencil8,  PixelFormat.DepthStencil,   PixelType.Float32UnsignedInt248Rev));
            Add(Format.R8G8B8A8Srgb,        new FormatInfo(4, false, false, InternalFormat.Srgb8Alpha8,       PixelFormat.Rgba,           PixelType.UnsignedByte));
            Add(Format.R4G4B4A4Unorm,       new FormatInfo(4, true,  false, InternalFormat.Rgba4,             PixelFormat.Rgba,           PixelType.UnsignedShort4444Rev));
            Add(Format.R5G5B5X1Unorm,       new FormatInfo(4, true,  false, InternalFormat.Rgb5,              PixelFormat.Rgb,            PixelType.UnsignedShort1555Rev));
            Add(Format.R5G5B5A1Unorm,       new FormatInfo(4, true,  false, InternalFormat.Rgb5A1,            PixelFormat.Rgba,           PixelType.UnsignedShort1555Rev));
            Add(Format.R5G6B5Unorm,         new FormatInfo(3, true,  false, InternalFormat.Rgb565,            PixelFormat.Rgb,            PixelType.UnsignedShort565Rev));
            Add(Format.R10G10B10A2Unorm,    new FormatInfo(4, true,  false, InternalFormat.Rgb10A2,           PixelFormat.Rgba,           PixelType.UnsignedInt2101010Rev));
            Add(Format.R10G10B10A2Uint,     new FormatInfo(4, false, false, InternalFormat.Rgb10A2ui,         PixelFormat.RgbaInteger,    PixelType.UnsignedInt2101010Rev));
            Add(Format.R11G11B10Float,      new FormatInfo(3, false, false, InternalFormat.R11fG11fB10f,      PixelFormat.Rgb,            PixelType.UnsignedInt10f11f11fRev));
            Add(Format.R9G9B9E5Float,       new FormatInfo(3, false, false, InternalFormat.Rgb9E5,            PixelFormat.Rgb,            PixelType.UnsignedInt5999Rev));
            Add(Format.Bc1RgbaUnorm,        new FormatInfo(4, true,  false, InternalFormat.CompressedRgbaS3TCDxt1Ext));
            Add(Format.Bc2Unorm,            new FormatInfo(4, true,  false, InternalFormat.CompressedRgbaS3TCDxt3Ext));
            Add(Format.Bc3Unorm,            new FormatInfo(4, true,  false, InternalFormat.CompressedRgbaS3TCDxt5Ext));
            Add(Format.Bc1RgbaSrgb,         new FormatInfo(4, true,  false, InternalFormat.CompressedSrgbAlphaS3TCDxt1Ext));
            Add(Format.Bc2Srgb,             new FormatInfo(4, false, false, InternalFormat.CompressedSrgbAlphaS3TCDxt3Ext));
            Add(Format.Bc3Srgb,             new FormatInfo(4, false, false, InternalFormat.CompressedSrgbAlphaS3TCDxt5Ext));
            Add(Format.Bc4Unorm,            new FormatInfo(1, true,  false, InternalFormat.CompressedRedRgtc1));
            Add(Format.Bc4Snorm,            new FormatInfo(1, true,  false, InternalFormat.CompressedSignedRedRgtc1));
            Add(Format.Bc5Unorm,            new FormatInfo(2, true,  false, InternalFormat.CompressedRGRgtc2));
            Add(Format.Bc5Snorm,            new FormatInfo(2, true,  false, InternalFormat.CompressedSignedRGRgtc2));
            Add(Format.Bc7Unorm,            new FormatInfo(4, true,  false, InternalFormat.CompressedRgbaBptcUnorm));
            Add(Format.Bc7Srgb,             new FormatInfo(4, false, false, InternalFormat.CompressedSrgbAlphaBptcUnorm));
            Add(Format.Bc6HSfloat,          new FormatInfo(4, false, false, InternalFormat.CompressedRgbBptcSignedFloat));
            Add(Format.Bc6HUfloat,          new FormatInfo(4, false, false, InternalFormat.CompressedRgbBptcUnsignedFloat));
            Add(Format.Etc2RgbUnorm,        new FormatInfo(4, false, false, InternalFormat.CompressedRgb8Etc2));
            Add(Format.Etc2RgbaUnorm,       new FormatInfo(4, false, false, InternalFormat.CompressedRgba8Etc2Eac));
            Add(Format.Etc2RgbPtaUnorm,     new FormatInfo(4, false, false, InternalFormat.CompressedRgb8PunchthroughAlpha1Etc2));
            Add(Format.Etc2RgbSrgb,         new FormatInfo(4, false, false, InternalFormat.CompressedSrgb8Etc2));
            Add(Format.Etc2RgbaSrgb,        new FormatInfo(4, false, false, InternalFormat.CompressedSrgb8Alpha8Etc2Eac));
            Add(Format.Etc2RgbPtaSrgb,      new FormatInfo(4, false, false, InternalFormat.CompressedSrgb8PunchthroughAlpha1Etc2));
            Add(Format.R8Uscaled,           new FormatInfo(1, false, true,  InternalFormat.R8ui,              PixelFormat.RedInteger,     PixelType.UnsignedByte));
            Add(Format.R8Sscaled,           new FormatInfo(1, false, true,  InternalFormat.R8i,               PixelFormat.RedInteger,     PixelType.Byte));
            Add(Format.R16Uscaled,          new FormatInfo(1, false, true,  InternalFormat.R16ui,             PixelFormat.RedInteger,     PixelType.UnsignedShort));
            Add(Format.R16Sscaled,          new FormatInfo(1, false, true,  InternalFormat.R16i,              PixelFormat.RedInteger,     PixelType.Short));
            Add(Format.R32Uscaled,          new FormatInfo(1, false, true,  InternalFormat.R32ui,             PixelFormat.RedInteger,     PixelType.UnsignedInt));
            Add(Format.R32Sscaled,          new FormatInfo(1, false, true,  InternalFormat.R32i,              PixelFormat.RedInteger,     PixelType.Int));
            Add(Format.R8G8Uscaled,         new FormatInfo(2, false, true,  InternalFormat.RG8ui,             PixelFormat.RGInteger,      PixelType.UnsignedByte));
            Add(Format.R8G8Sscaled,         new FormatInfo(2, false, true,  InternalFormat.RG8i,              PixelFormat.RGInteger,      PixelType.Byte));
            Add(Format.R16G16Uscaled,       new FormatInfo(2, false, true,  InternalFormat.RG16ui,            PixelFormat.RGInteger,      PixelType.UnsignedShort));
            Add(Format.R16G16Sscaled,       new FormatInfo(2, false, true,  InternalFormat.RG16i,             PixelFormat.RGInteger,      PixelType.Short));
            Add(Format.R32G32Uscaled,       new FormatInfo(2, false, true,  InternalFormat.RG32ui,            PixelFormat.RGInteger,      PixelType.UnsignedInt));
            Add(Format.R32G32Sscaled,       new FormatInfo(2, false, true,  InternalFormat.RG32i,             PixelFormat.RGInteger,      PixelType.Int));
            Add(Format.R8G8B8Uscaled,       new FormatInfo(3, false, true,  InternalFormat.Rgb8ui,            PixelFormat.RgbInteger,     PixelType.UnsignedByte));
            Add(Format.R8G8B8Sscaled,       new FormatInfo(3, false, true,  InternalFormat.Rgb8i,             PixelFormat.RgbInteger,     PixelType.Byte));
            Add(Format.R16G16B16Uscaled,    new FormatInfo(3, false, true,  InternalFormat.Rgb16ui,           PixelFormat.RgbInteger,     PixelType.UnsignedShort));
            Add(Format.R16G16B16Sscaled,    new FormatInfo(3, false, true,  InternalFormat.Rgb16i,            PixelFormat.RgbInteger,     PixelType.Short));
            Add(Format.R32G32B32Uscaled,    new FormatInfo(3, false, true,  InternalFormat.Rgb32ui,           PixelFormat.RgbInteger,     PixelType.UnsignedInt));
            Add(Format.R32G32B32Sscaled,    new FormatInfo(3, false, true,  InternalFormat.Rgb32i,            PixelFormat.RgbInteger,     PixelType.Int));
            Add(Format.R8G8B8A8Uscaled,     new FormatInfo(4, false, true,  InternalFormat.Rgba8ui,           PixelFormat.RgbaInteger,    PixelType.UnsignedByte));
            Add(Format.R8G8B8A8Sscaled,     new FormatInfo(4, false, true,  InternalFormat.Rgba8i,            PixelFormat.RgbaInteger,    PixelType.Byte));
            Add(Format.R16G16B16A16Uscaled, new FormatInfo(4, false, true,  InternalFormat.Rgba16ui,          PixelFormat.RgbaInteger,    PixelType.UnsignedShort));
            Add(Format.R16G16B16A16Sscaled, new FormatInfo(4, false, true,  InternalFormat.Rgba16i,           PixelFormat.RgbaInteger,    PixelType.Short));
            Add(Format.R32G32B32A32Uscaled, new FormatInfo(4, false, true,  InternalFormat.Rgba32ui,          PixelFormat.RgbaInteger,    PixelType.UnsignedInt));
            Add(Format.R32G32B32A32Sscaled, new FormatInfo(4, false, true,  InternalFormat.Rgba32i,           PixelFormat.RgbaInteger,    PixelType.Int));
            Add(Format.R10G10B10A2Snorm,    new FormatInfo(4, true,  false, InternalFormat.Rgb10A2,           PixelFormat.Rgba,           (PixelType)GLEnum.Int2101010Rev));
            Add(Format.R10G10B10A2Sint,     new FormatInfo(4, false, false, InternalFormat.Rgb10A2,           PixelFormat.RgbaInteger,    (PixelType)GLEnum.Int2101010Rev));
            Add(Format.R10G10B10A2Uscaled,  new FormatInfo(4, false, true,  InternalFormat.Rgb10A2ui,         PixelFormat.RgbaInteger,    PixelType.UnsignedInt2101010Rev));
            Add(Format.R10G10B10A2Sscaled,  new FormatInfo(4, false, true,  InternalFormat.Rgb10A2,           PixelFormat.RgbaInteger,    PixelType.UnsignedInt2101010Rev));
            Add(Format.Astc4x4Unorm,        new FormatInfo(4, true,  false, InternalFormat.CompressedRgbaAstc4x4Khr));
            Add(Format.Astc5x4Unorm,        new FormatInfo(4, true,  false, InternalFormat.CompressedRgbaAstc5x4Khr));
            Add(Format.Astc5x5Unorm,        new FormatInfo(4, true,  false, InternalFormat.CompressedRgbaAstc5x5Khr));
            Add(Format.Astc6x5Unorm,        new FormatInfo(4, true,  false, InternalFormat.CompressedRgbaAstc6x5Khr));
            Add(Format.Astc6x6Unorm,        new FormatInfo(4, true,  false, InternalFormat.CompressedRgbaAstc6x6Khr));
            Add(Format.Astc8x5Unorm,        new FormatInfo(4, true,  false, InternalFormat.CompressedRgbaAstc8x5Khr));
            Add(Format.Astc8x6Unorm,        new FormatInfo(4, true,  false, InternalFormat.CompressedRgbaAstc8x6Khr));
            Add(Format.Astc8x8Unorm,        new FormatInfo(4, true,  false, InternalFormat.CompressedRgbaAstc8x8Khr));
            Add(Format.Astc10x5Unorm,       new FormatInfo(4, true,  false, InternalFormat.CompressedRgbaAstc10x5Khr));
            Add(Format.Astc10x6Unorm,       new FormatInfo(4, true,  false, InternalFormat.CompressedRgbaAstc10x6Khr));
            Add(Format.Astc10x8Unorm,       new FormatInfo(4, true,  false, InternalFormat.CompressedRgbaAstc10x8Khr));
            Add(Format.Astc10x10Unorm,      new FormatInfo(4, true,  false, InternalFormat.CompressedRgbaAstc10x10Khr));
            Add(Format.Astc12x10Unorm,      new FormatInfo(4, true,  false, InternalFormat.CompressedRgbaAstc12x10Khr));
            Add(Format.Astc12x12Unorm,      new FormatInfo(4, true,  false, InternalFormat.CompressedRgbaAstc12x12Khr));
            Add(Format.Astc4x4Srgb,         new FormatInfo(4, false, false, InternalFormat.CompressedSrgb8Alpha8Astc4x4Khr));
            Add(Format.Astc5x4Srgb,         new FormatInfo(4, false, false, InternalFormat.CompressedSrgb8Alpha8Astc5x4Khr));
            Add(Format.Astc5x5Srgb,         new FormatInfo(4, false, false, InternalFormat.CompressedSrgb8Alpha8Astc5x5Khr));
            Add(Format.Astc6x5Srgb,         new FormatInfo(4, false, false, InternalFormat.CompressedSrgb8Alpha8Astc6x5Khr));
            Add(Format.Astc6x6Srgb,         new FormatInfo(4, false, false, InternalFormat.CompressedSrgb8Alpha8Astc6x6Khr));
            Add(Format.Astc8x5Srgb,         new FormatInfo(4, false, false, InternalFormat.CompressedSrgb8Alpha8Astc8x5Khr));
            Add(Format.Astc8x6Srgb,         new FormatInfo(4, false, false, InternalFormat.CompressedSrgb8Alpha8Astc8x6Khr));
            Add(Format.Astc8x8Srgb,         new FormatInfo(4, false, false, InternalFormat.CompressedSrgb8Alpha8Astc8x8Khr));
            Add(Format.Astc10x5Srgb,        new FormatInfo(4, false, false, InternalFormat.CompressedSrgb8Alpha8Astc10x5Khr));
            Add(Format.Astc10x6Srgb,        new FormatInfo(4, false, false, InternalFormat.CompressedSrgb8Alpha8Astc10x6Khr));
            Add(Format.Astc10x8Srgb,        new FormatInfo(4, false, false, InternalFormat.CompressedSrgb8Alpha8Astc10x8Khr));
            Add(Format.Astc10x10Srgb,       new FormatInfo(4, false, false, InternalFormat.CompressedSrgb8Alpha8Astc10x10Khr));
            Add(Format.Astc12x10Srgb,       new FormatInfo(4, false, false, InternalFormat.CompressedSrgb8Alpha8Astc12x10Khr));
            Add(Format.Astc12x12Srgb,       new FormatInfo(4, false, false, InternalFormat.CompressedSrgb8Alpha8Astc12x12Khr));
            Add(Format.B5G6R5Unorm,         new FormatInfo(3, true,  false, InternalFormat.Rgb565,            PixelFormat.Rgb,            PixelType.UnsignedShort565Rev));
            Add(Format.B5G5R5A1Unorm,       new FormatInfo(4, true,  false, InternalFormat.Rgb5A1,            PixelFormat.Rgba,           PixelType.UnsignedShort1555Rev));
            Add(Format.A1B5G5R5Unorm,       new FormatInfo(4, true,  false, InternalFormat.Rgb5A1,            PixelFormat.Rgba,           PixelType.UnsignedShort5551));
            Add(Format.B8G8R8A8Unorm,       new FormatInfo(4, true,  false, InternalFormat.Rgba8,             PixelFormat.Rgba,           PixelType.UnsignedByte));
            Add(Format.B8G8R8A8Srgb,        new FormatInfo(4, false, false, InternalFormat.Srgb8Alpha8,       PixelFormat.Rgba,           PixelType.UnsignedByte));
            Add(Format.B10G10R10A2Unorm,    new FormatInfo(4, false, false, InternalFormat.Rgb10A2,           PixelFormat.Rgba,           PixelType.UnsignedInt2101010Rev));

            Add(Format.R8Unorm,           SizedInternalFormat.R8);
            Add(Format.R8Uint,            SizedInternalFormat.R8ui);
            Add(Format.R8Sint,            SizedInternalFormat.R8i);
            Add(Format.R16Float,          SizedInternalFormat.R16f);
            Add(Format.R16Unorm,          SizedInternalFormat.R16);
            Add(Format.R16Snorm,          SizedInternalFormat.R16SNorm);
            Add(Format.R16Uint,           SizedInternalFormat.R16ui);
            Add(Format.R16Sint,           SizedInternalFormat.R16i);
            Add(Format.R32Float,          SizedInternalFormat.R32f);
            Add(Format.R32Uint,           SizedInternalFormat.R32ui);
            Add(Format.R32Sint,           SizedInternalFormat.R32i);
            Add(Format.R8G8Unorm,         SizedInternalFormat.RG8);
            Add(Format.R8G8Snorm,         SizedInternalFormat.RG8SNorm);
            Add(Format.R8G8Uint,          SizedInternalFormat.RG8ui);
            Add(Format.R8G8Sint,          SizedInternalFormat.RG8i);
            Add(Format.R16G16Float,       SizedInternalFormat.RG16f);
            Add(Format.R16G16Unorm,       SizedInternalFormat.RG16);
            Add(Format.R16G16Snorm,       SizedInternalFormat.RG16SNorm);
            Add(Format.R16G16Uint,        SizedInternalFormat.RG16ui);
            Add(Format.R16G16Sint,        SizedInternalFormat.RG16i);
            Add(Format.R32G32Float,       SizedInternalFormat.RG32f);
            Add(Format.R32G32Uint,        SizedInternalFormat.RG32ui);
            Add(Format.R32G32Sint,        SizedInternalFormat.RG32i);
            Add(Format.R8G8B8A8Unorm,     SizedInternalFormat.Rgba8);
            Add(Format.R8G8B8A8Snorm,     SizedInternalFormat.Rgba8SNorm);
            Add(Format.R8G8B8A8Uint,      SizedInternalFormat.Rgba8ui);
            Add(Format.R8G8B8A8Sint,      SizedInternalFormat.Rgba8i);
            Add(Format.R16G16B16A16Float, SizedInternalFormat.Rgba16f);
            Add(Format.R16G16B16A16Unorm, SizedInternalFormat.Rgba16);
            Add(Format.R16G16B16A16Snorm, SizedInternalFormat.Rgba16SNorm);
            Add(Format.R16G16B16A16Uint,  SizedInternalFormat.Rgba16ui);
            Add(Format.R16G16B16A16Sint,  SizedInternalFormat.Rgba16i);
            Add(Format.R32G32B32A32Float, SizedInternalFormat.Rgba32f);
            Add(Format.R32G32B32A32Uint,  SizedInternalFormat.Rgba32ui);
            Add(Format.R32G32B32A32Sint,  SizedInternalFormat.Rgba32i);
            Add(Format.R8G8B8A8Srgb,      SizedInternalFormat.Rgba8);
            Add(Format.R10G10B10A2Unorm,  SizedInternalFormat.Rgb10A2);
            Add(Format.R10G10B10A2Uint,   SizedInternalFormat.Rgb10A2ui);
            Add(Format.R11G11B10Float,    SizedInternalFormat.R11fG11fB10f);
#pragma warning restore IDE0055
        }

        private static void Add(Format format, FormatInfo info)
        {
            _table[(int)format] = info;
        }

        private static void Add(Format format, SizedInternalFormat sif)
        {
            _tableImage[(int)format] = sif;
        }

        public static FormatInfo GetFormatInfo(Format format)
        {
            return _table[(int)format];
        }

        public static SizedInternalFormat GetImageFormat(Format format)
        {
            return _tableImage[(int)format];
        }

        public static bool IsPackedDepthStencil(Format format)
        {
            return format == Format.D24UnormS8Uint ||
                   format == Format.D32FloatS8Uint ||
                   format == Format.S8UintD24Unorm;
        }

        public static bool IsDepthOnly(Format format)
        {
            return format == Format.D16Unorm || format == Format.D32Float || format == Format.X8UintD24Unorm;
        }
    }
}
