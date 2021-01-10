using Ryujinx.Graphics.GAL;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Image
{
    /// <summary>
    /// Contains format tables, for texture and vertex attribute formats.
    /// </summary>
    static class FormatTable
    {
        private enum GpuFormat
        {
            R8                  = 0x1D,
            R16                 = 0x1B,
            R32                 = 0x0F,
            G8R8                = 0x18,
            R16G16              = 0x0C,
            R32G32              = 0x04,
            R32G32B32           = 0x02,
            A8B8G8R8            = 0x08,
            R16G16B16A16        = 0x03,
            R32G32B32A32        = 0x01,
            X8Z24X20V4S8COV4R4V = 0x3A,
            ZF32                = 0x2F,
            G24R8               = 0x0E,
            Z24S8               = 0x29,
            R32B24G8            = 0x05,
            ZF32X24S8           = 0x30,
            A4B4G4R4            = 0x12,
            A1B5G5R5            = 0x14,
            B5G6R5              = 0x15,
            A2B10G10R10         = 0x09,
            BF10GF11RF11        = 0x21,
            E5B9G9R9SHAREDEXP   = 0x20,
            DXT1                = 0x24,
            DXT23               = 0x25,
            DXT45               = 0x26,
            A5B5G5R1            = 0x13,
            DXN1                = 0x27,
            DXN2                = 0x28,
            BC7U                = 0x17,
            ETC2RGBA            = 0x0B,
            RTDOUBLEBIND        = 0x10,
            RTTYPESMISMATCH     = 0x11,
            ASTC2D4X4           = 0x40,
            ASTC2D5X4           = 0x50,
            ASTC2D5X5           = 0x41,
            ASTC2D6X5           = 0x51,
            ASTC2D6X6           = 0x42,
            ASTC2D8X5           = 0x55,
            ASTC2D8X6           = 0x52,
            ASTC2D8X8           = 0x44,
            ASTC2D10X5          = 0x56,
            ASTC2D10X6          = 0x57,
            ASTC2D10X8          = 0x53,
            ASTC2D10X10         = 0x45,
            ASTC2D12X10         = 0x54,
            ASTC2D12X12         = 0x46,
            X8Z24               = 0x2a,
            S8Z24               = 0x2b,
            G8B8G8R8            = 0x22,
            B8G8R8G8            = 0x23,

            RSnorm = 0x1 << 7,
            GSnorm = 0x1 << 10,
            BSnorm = 0x1 << 13,
            ASnorm = 0x1 << 16,

            RUnorm = 0x2 << 7,
            GUnorm = 0x2 << 10,
            BUnorm = 0x2 << 13,
            AUnorm = 0x2 << 16,

            RSint = 0x3 << 7,
            GSint = 0x3 << 10,
            BSint = 0x3 << 13,
            ASint = 0x3 << 16,

            RUint = 0x4 << 7,
            GUint = 0x4 << 10,
            BUint = 0x4 << 13,
            AUint = 0x4 << 16,

            RSNormForceFP16 = 0x5 << 7,
            GSNormForceFP16 = 0x5 << 10,
            BSNormForceFP16 = 0x5 << 13,
            ASNormForceFP16 = 0x5 << 16,

            RFloat = 0x7 << 7,
            GFloat = 0x7 << 10,
            BFloat = 0x7 << 13,
            AFloat = 0x7 << 16,

            // Texture Names
            R8Unorm                       = R8                  | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x2491d
            R8Snorm                       = R8                  | RSnorm | GSnorm | BSnorm | ASnorm         , // 0x1249d
            R8Uint                        = R8                  | RUint  | GUint  | BUint  | AUint          , // 0x4921d
            R8Sint                        = R8                  | RSint  | GSint  | BSint  | ASint          , // 0x36d9d
            R16Float                      = R16                 | RFloat | GFloat | BFloat | AFloat         , // 0x7ff9b
            R16Unorm                      = R16                 | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x2491b
            R16Sorm                       = R16                 | RSnorm | GSnorm | BSnorm | ASnorm         , // 0x1249b
            R16Uint                       = R16                 | RUint  | GUint  | BUint  | AUint          , // 0x4921b
            R16Sint                       = R16                 | RSint  | GSint  | BSint  | ASint          , // 0x36d9b
            R32Float                      = R32                 | RFloat | GFloat | BFloat | AFloat         , // 0x7ff8f
            R32Uint                       = R32                 | RUint  | GUint  | BUint  | AUint          , // 0x4920f
            R32Sint                       = R32                 | RSint  | GSint  | BSint  | ASint          , // 0x36d8f
            G8R8Unorm                     = G8R8                | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24918
            G8R8Snorm                     = G8R8                | RSnorm | GSnorm | BSnorm | ASnorm         , // 0x12498
            G8R8Uint                      = G8R8                | RUint  | GUint  | BUint  | AUint          , // 0x49218
            G8R8Sint                      = G8R8                | RSint  | GSint  | BSint  | ASint          , // 0x36d98
            R16G16Float                   = R16G16              | RFloat | GFloat | BFloat | AFloat         , // 0x7ff8c
            R16G16Unorm                   = R16G16              | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x2490c
            R16G16Snorm                   = R16G16              | RSnorm | GSnorm | BSnorm | ASnorm         , // 0x1248c
            R16G16Uint                    = R16G16              | RUint  | GUint  | BUint  | AUint          , // 0x4920c
            R16G16Sint                    = R16G16              | RSint  | GSint  | BSint  | ASint          , // 0x36d8c
            R32G32Float                   = R32G32              | RFloat | GFloat | BFloat | AFloat         , // 0x7ff84
            R32G32Uint                    = R32G32              | RUint  | GUint  | BUint  | AUint          , // 0x49204
            R32G32Sint                    = R32G32              | RSint  | GSint  | BSint  | ASint          , // 0x36d84
            R32G32B32Float                = R32G32B32           | RFloat | GFloat | BFloat | AFloat         , // 0x7ff82
            R32G32B32Uint                 = R32G32B32           | RUint  | GUint  | BUint  | AUint          , // 0x49202
            R32G32B32Sint                 = R32G32B32           | RSint  | GSint  | BSint  | ASint          , // 0x36d82
            A8B8G8R8Unorm                 = A8B8G8R8            | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24908
            A8B8G8R8Snorm                 = A8B8G8R8            | RSnorm | GSnorm | BSnorm | ASnorm         , // 0x12488
            A8B8G8R8Uint                  = A8B8G8R8            | RUint  | GUint  | BUint  | AUint          , // 0x49208
            A8B8G8R8Sint                  = A8B8G8R8            | RSint  | GSint  | BSint  | ASint          , // 0x36d88
            R16G16B16A16Float             = R16G16B16A16        | RFloat | GFloat | BFloat | AFloat         , // 0x7ff83
            R16G16B16A16Unorm             = R16G16B16A16        | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24903
            R16G16B16A16Snorm             = R16G16B16A16        | RSnorm | GSnorm | BSnorm | ASnorm         , // 0x12483
            R16G16B16A16Uint              = R16G16B16A16        | RUint  | GUint  | BUint  | AUint          , // 0x49203
            R16G16B16A16Sint              = R16G16B16A16        | RSint  | GSint  | BSint  | ASint          , // 0x36d83
            R32G32B32A32Float             = R32G32B32A32        | RFloat | GFloat | BFloat | AFloat         , // 0x7ff81
            R32G32B32A32Uint              = R32G32B32A32        | RUint  | GUint  | BUint  | AUint          , // 0x49201
            R32G32B32A32Sint              = R32G32B32A32        | RSint  | GSint  | BSint  | ASint          , // 0x36d81
            X8Z24X20V4S8COV4R4VUnorm      = X8Z24X20V4S8COV4R4V | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x2493a
            ZF32Float                     = ZF32                | RFloat | GFloat | BFloat | AFloat         , // 0x7ffaf
            G24R8UnitGunormUnormUnorm     = G24R8               | RUint  | GUnorm | BUnorm | AUnorm         , // 0x24a0e
            Z24S8UintGunormUnormUnorm     = Z24S8               | RUint  | GUnorm | BUnorm | AUnorm         , // 0x24a29
            Z24S8UintGunormUnintUint      = Z24S8               | RUint  | GUnorm | BUint  | AUint          , // 0x48a29
            R32B24G8FloatUintUnormUnorm   = R32B24G8            | RFloat | GUint  | BUnorm | AUnorm         , // 0x25385
            ZF32X24S8FloatUintUnormUnorm  = ZF32X24S8           | RFloat | GUint  | BUnorm | AUnorm         , // 0x253b0
            R32G32Snorm                   = R32G32              | RSnorm | GSnorm | BSnorm | ASNormForceFP16, // 0xa4908
            A4B4G4R4Unorm                 = A4B4G4R4            | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24912
            A1B5G5R5Unorm                 = A1B5G5R5            | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24914
            B5G6R5Unorm                   = B5G6R5              | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24915
            A2B10G10R10Unorm              = A2B10G10R10         | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24909
            A2B10G10R10Uint               = A2B10G10R10         | RUint  | GUint  | BUint  | AUint          , // 0x49209
            BF10GF11RF11Float             = BF10GF11RF11        | RFloat | GFloat | BFloat | AFloat         , // 0x7ffa1
            E5B9G9R9SHAREDEXPFloat        = E5B9G9R9SHAREDEXP   | RFloat | GFloat | BFloat | AFloat         , // 0x7ffa0
            DXT1Unorm                     = DXT1                | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24924
            DXT23Unorm                    = DXT23               | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24925
            DXT45Unorm                    = DXT45               | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24926
            A4B4G4R4Snorm_First           = A4B4G4R4            | RSnorm | GSnorm | BSnorm | ASNormForceFP16, // 0xa4924
            // A4B4G4R4Snorm_Second          = A4B4G4R4            | RSnorm | GSnorm | BSnorm | ASNormForceFP16, // 0xa4925
            A5B5G5R1Snorm                 = A5B5G5R1            | RSnorm | GSnorm | BSnorm | ASNormForceFP16, // 0xa4926
            DXN1Unorm                     = DXN1                | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24927
            DXN1Snorm                     = DXN1                | RSnorm | GSnorm | BSnorm | ASnorm         , // 0x124a7
            DXN2Unorm                     = DXN2                | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24928
            DXN2Snorm                     = DXN2                | RSnorm | GSnorm | BSnorm | ASnorm         , // 0x124a8
            BC7UUnorm                     = BC7U                | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24917
            ETC2RGBASnorm                 = ETC2RGBA            | RSnorm | GSnorm | BSnorm | ASNormForceFP16, // 0xa4917
            RTDOUBLEBINDFloat             = RTDOUBLEBIND        | RFloat | GFloat | BFloat | AFloat         , // 0x7ff90
            RTTYPESMISMATCHFloat          = RTTYPESMISMATCH     | RFloat | GFloat | BFloat | AFloat         , // 0x7ff91
            ETC2RGBAUnorm                 = ETC2RGBA            | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x2490b
            R32B24G8Snorm                 = R32B24G8            | RSnorm | GSnorm | BSnorm | ASNormForceFP16, // 0xa490b
            ASTC2D4X4Unorm                = ASTC2D4X4           | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24940
            ASTC2D5X4Unorm                = ASTC2D5X4           | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24950
            ASTC2D5X5Unorm                = ASTC2D5X5           | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24941
            ASTC2D6X5Unorm                = ASTC2D6X5           | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24951
            ASTC2D6X6Unorm                = ASTC2D6X6           | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24942
            ASTC2D8X5Unorm                = ASTC2D8X5           | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24955
            ASTC2D8X6Unorm                = ASTC2D8X6           | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24952
            ASTC2D8X8Unorm                = ASTC2D8X8           | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24944
            ASTC2D10X5Unorm               = ASTC2D10X5          | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24956
            ASTC2D10X6Unorm               = ASTC2D10X6          | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24957
            ASTC2D10X8Unorm               = ASTC2D10X8          | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24953
            ASTC2D10X10Unorm              = ASTC2D10X10         | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24945
            ASTC2D12X10Unorm              = ASTC2D12X10         | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24954
            ASTC2D12X12Unorm              = ASTC2D12X12         | RUnorm | GUnorm | BUnorm | AUnorm         , // 0x24946
            E5B9G9R9SHAREDEXPSnorm_First  = E5B9G9R9SHAREDEXP   | RSnorm | GSnorm | BSnorm | ASNormForceFP16, // 0xa4940
            DXN2Snorm_First               = DXN2                | RSnorm | GSnorm | BSnorm | ASNormForceFP16, // 0xa4950
            // E5B9G9R9SHAREDEXPSnorm_Second = E5B9G9R9SHAREDEXP   | RSnorm | GSnorm | BSnorm | ASNormForceFP16, // 0xa4941
            // DXN2Snorm_Second              = DXN2                | RSnorm | GSnorm | BSnorm | ASNormForceFP16, // 0xa4951
            BF10GF11RF11Snorm             = BF10GF11RF11        | RSnorm | GSnorm | BSnorm | ASNormForceFP16, // 0xa4942
            X8Z24Snorm_First              = X8Z24               | RSnorm | GSnorm | BSnorm | ASNormForceFP16, // 0xa4955
            Z24S8Snorm_First              = Z24S8               | RSnorm | GSnorm | BSnorm | ASNormForceFP16, // 0xa4952
            G8B8G8R8Snorm_First           = G8B8G8R8            | RSnorm | GSnorm | BSnorm | ASNormForceFP16, // 0xa4944
            S8Z24Snorm_First              = S8Z24               | RSnorm | GSnorm | BSnorm | ASNormForceFP16, // 0xa4956
            // S8Z24Snorm_Second             = S8Z24               | RSnorm | GSnorm | BSnorm | ASNormForceFP16, // 0xa4957
            // Z24S8Snorm_Second             = Z24S8               | RSnorm | GSnorm | BSnorm | ASNormForceFP16, // 0xa4953
            // G8B8G8R8Snorm_Second          = G8B8G8R8            | RSnorm | GSnorm | BSnorm | ASNormForceFP16, // 0xa4945
            // X8Z24Snorm_Second             = X8Z24               | RSnorm | GSnorm | BSnorm | ASNormForceFP16, // 0xa4954
            B8G8R8G8Snorm                 = B8G8R8G8            | RSnorm | GSnorm | BSnorm | ASNormForceFP16, // 0xa4946
            A5B5G5R1Unorm                 = A5B5G5R1            | RUnorm | GUnorm | BUnorm | AUnorm           // 0x24913
        }

        private static Dictionary<uint, FormatInfo> _textureFormats = new Dictionary<uint, FormatInfo>()
        {
            { (uint) GpuFormat.R8Unorm                      , new FormatInfo(Format.R8Unorm          , 1 , 1 , 1 , 1) },
            { (uint) GpuFormat.R8Snorm                      , new FormatInfo(Format.R8Snorm          , 1 , 1 , 1 , 1) },
            { (uint) GpuFormat.R8Uint                       , new FormatInfo(Format.R8Uint           , 1 , 1 , 1 , 1) },
            { (uint) GpuFormat.R8Sint                       , new FormatInfo(Format.R8Sint           , 1 , 1 , 1 , 1) },
            { (uint) GpuFormat.R16Float                     , new FormatInfo(Format.R16Float         , 1 , 1 , 2 , 1) },
            { (uint) GpuFormat.R16Unorm                     , new FormatInfo(Format.R16Unorm         , 1 , 1 , 2 , 1) },
            { (uint) GpuFormat.R16Sorm                      , new FormatInfo(Format.R16Snorm         , 1 , 1 , 2 , 1) },
            { (uint) GpuFormat.R16Uint                      , new FormatInfo(Format.R16Uint          , 1 , 1 , 2 , 1) },
            { (uint) GpuFormat.R16Sint                      , new FormatInfo(Format.R16Sint          , 1 , 1 , 2 , 1) },
            { (uint) GpuFormat.R32Float                     , new FormatInfo(Format.R32Float         , 1 , 1 , 4 , 1) },
            { (uint) GpuFormat.R32Uint                      , new FormatInfo(Format.R32Uint          , 1 , 1 , 4 , 1) },
            { (uint) GpuFormat.R32Sint                      , new FormatInfo(Format.R32Sint          , 1 , 1 , 4 , 1) },
            { (uint) GpuFormat.G8R8Unorm                    , new FormatInfo(Format.R8G8Unorm        , 1 , 1 , 2 , 2) },
            { (uint) GpuFormat.G8R8Snorm                    , new FormatInfo(Format.R8G8Snorm        , 1 , 1 , 2 , 2) },
            { (uint) GpuFormat.G8R8Uint                     , new FormatInfo(Format.R8G8Uint         , 1 , 1 , 2 , 2) },
            { (uint) GpuFormat.G8R8Sint                     , new FormatInfo(Format.R8G8Sint         , 1 , 1 , 2 , 2) },
            { (uint) GpuFormat.R16G16Float                  , new FormatInfo(Format.R16G16Float      , 1 , 1 , 4 , 2) },
            { (uint) GpuFormat.R16G16Unorm                  , new FormatInfo(Format.R16G16Unorm      , 1 , 1 , 4 , 2) },
            { (uint) GpuFormat.R16G16Snorm                  , new FormatInfo(Format.R16G16Snorm      , 1 , 1 , 4 , 2) },
            { (uint) GpuFormat.R16G16Uint                   , new FormatInfo(Format.R16G16Uint       , 1 , 1 , 4 , 2) },
            { (uint) GpuFormat.R16G16Sint                   , new FormatInfo(Format.R16G16Sint       , 1 , 1 , 4 , 2) },
            { (uint) GpuFormat.R32G32Float                  , new FormatInfo(Format.R32G32Float      , 1 , 1 , 8 , 2) },
            { (uint) GpuFormat.R32G32Uint                   , new FormatInfo(Format.R32G32Uint       , 1 , 1 , 8 , 2) },
            { (uint) GpuFormat.R32G32Sint                   , new FormatInfo(Format.R32G32Sint       , 1 , 1 , 8 , 2) },
            { (uint) GpuFormat.R32G32B32Float               , new FormatInfo(Format.R32G32B32Float   , 1 , 1 , 12, 3) },
            { (uint) GpuFormat.R32G32B32Uint                , new FormatInfo(Format.R32G32B32Uint    , 1 , 1 , 12, 3) },
            { (uint) GpuFormat.R32G32B32Sint                , new FormatInfo(Format.R32G32B32Sint    , 1 , 1 , 12, 3) },
            { (uint) GpuFormat.A8B8G8R8Unorm                , new FormatInfo(Format.R8G8B8A8Unorm    , 1 , 1 , 4 , 4) },
            { (uint) GpuFormat.A8B8G8R8Snorm                , new FormatInfo(Format.R8G8B8A8Snorm    , 1 , 1 , 4 , 4) },
            { (uint) GpuFormat.A8B8G8R8Uint                 , new FormatInfo(Format.R8G8B8A8Uint     , 1 , 1 , 4 , 4) },
            { (uint) GpuFormat.A8B8G8R8Sint                 , new FormatInfo(Format.R8G8B8A8Sint     , 1 , 1 , 4 , 4) },
            { (uint) GpuFormat.R16G16B16A16Float            , new FormatInfo(Format.R16G16B16A16Float, 1 , 1 , 8 , 4) },
            { (uint) GpuFormat.R16G16B16A16Unorm            , new FormatInfo(Format.R16G16B16A16Unorm, 1 , 1 , 8 , 4) },
            { (uint) GpuFormat.R16G16B16A16Snorm            , new FormatInfo(Format.R16G16B16A16Snorm, 1 , 1 , 8 , 4) },
            { (uint) GpuFormat.R16G16B16A16Uint             , new FormatInfo(Format.R16G16B16A16Uint , 1 , 1 , 8 , 4) },
            { (uint) GpuFormat.R16G16B16A16Sint             , new FormatInfo(Format.R16G16B16A16Sint , 1 , 1 , 8 , 4) },
            { (uint) GpuFormat.R32G32B32A32Float            , new FormatInfo(Format.R32G32B32A32Float, 1 , 1 , 16, 4) },
            { (uint) GpuFormat.R32G32B32A32Uint             , new FormatInfo(Format.R32G32B32A32Uint , 1 , 1 , 16, 4) },
            { (uint) GpuFormat.R32G32B32A32Sint             , new FormatInfo(Format.R32G32B32A32Sint , 1 , 1 , 16, 4) },
            { (uint) GpuFormat.X8Z24X20V4S8COV4R4VUnorm     , new FormatInfo(Format.D16Unorm         , 1 , 1 , 2 , 1) },
            { (uint) GpuFormat.ZF32Float                    , new FormatInfo(Format.D32Float         , 1 , 1 , 4 , 1) },
            { (uint) GpuFormat.G24R8UnitGunormUnormUnorm    , new FormatInfo(Format.D24UnormS8Uint   , 1 , 1 , 4 , 2) },
            { (uint) GpuFormat.Z24S8UintGunormUnormUnorm    , new FormatInfo(Format.D24UnormS8Uint   , 1 , 1 , 4 , 2) },
            { (uint) GpuFormat.Z24S8UintGunormUnintUint     , new FormatInfo(Format.D24UnormS8Uint   , 1 , 1 , 4 , 2) },
            { (uint) GpuFormat.R32B24G8FloatUintUnormUnorm  , new FormatInfo(Format.D32FloatS8Uint   , 1 , 1 , 8 , 2) },
            { (uint) GpuFormat.ZF32X24S8FloatUintUnormUnorm , new FormatInfo(Format.D32FloatS8Uint   , 1 , 1 , 8 , 2) },
            { (uint) GpuFormat.R32G32Snorm                  , new FormatInfo(Format.R8G8B8A8Srgb     , 1 , 1 , 4 , 4) },
            { (uint) GpuFormat.A4B4G4R4Unorm                , new FormatInfo(Format.R4G4B4A4Unorm    , 1 , 1 , 2 , 4) },
            { (uint) GpuFormat.A1B5G5R5Unorm                , new FormatInfo(Format.R5G5B5A1Unorm    , 1 , 1 , 2 , 4) },
            { (uint) GpuFormat.B5G6R5Unorm                  , new FormatInfo(Format.R5G6B5Unorm      , 1 , 1 , 2 , 3) },
            { (uint) GpuFormat.A2B10G10R10Unorm             , new FormatInfo(Format.R10G10B10A2Unorm , 1 , 1 , 4 , 4) },
            { (uint) GpuFormat.A2B10G10R10Uint              , new FormatInfo(Format.R10G10B10A2Uint  , 1 , 1 , 4 , 4) },
            { (uint) GpuFormat.BF10GF11RF11Float            , new FormatInfo(Format.R11G11B10Float   , 1 , 1 , 4 , 3) },
            { (uint) GpuFormat.E5B9G9R9SHAREDEXPFloat       , new FormatInfo(Format.R9G9B9E5Float    , 1 , 1 , 4 , 4) },
            { (uint) GpuFormat.DXT1Unorm                    , new FormatInfo(Format.Bc1RgbaUnorm     , 4 , 4 , 8 , 4) },
            { (uint) GpuFormat.DXT23Unorm                   , new FormatInfo(Format.Bc2Unorm         , 4 , 4 , 16, 4) },
            { (uint) GpuFormat.DXT45Unorm                   , new FormatInfo(Format.Bc3Unorm         , 4 , 4 , 16, 4) },
            { (uint) GpuFormat.A4B4G4R4Snorm_First          , new FormatInfo(Format.Bc1RgbaSrgb      , 4 , 4 , 8 , 4) },
            // { (uint) GpuFormat.A4B4G4R4Snorm_Second         , new FormatInfo(Format.Bc2Srgb          , 4 , 4 , 16, 4) },
            { (uint) GpuFormat.A5B5G5R1Snorm                , new FormatInfo(Format.Bc3Srgb          , 4 , 4 , 16, 4) },
            { (uint) GpuFormat.DXN1Unorm                    , new FormatInfo(Format.Bc4Unorm         , 4 , 4 , 8 , 1) },
            { (uint) GpuFormat.DXN1Snorm                    , new FormatInfo(Format.Bc4Snorm         , 4 , 4 , 8 , 1) },
            { (uint) GpuFormat.DXN2Unorm                    , new FormatInfo(Format.Bc5Unorm         , 4 , 4 , 16, 2) },
            { (uint) GpuFormat.DXN2Snorm                    , new FormatInfo(Format.Bc5Snorm         , 4 , 4 , 16, 2) },
            { (uint) GpuFormat.BC7UUnorm                    , new FormatInfo(Format.Bc7Unorm         , 4 , 4 , 16, 4) },
            { (uint) GpuFormat.ETC2RGBASnorm                , new FormatInfo(Format.Bc7Srgb          , 4 , 4 , 16, 4) },
            { (uint) GpuFormat.RTDOUBLEBINDFloat            , new FormatInfo(Format.Bc6HSfloat       , 4 , 4 , 16, 4) },
            { (uint) GpuFormat.RTTYPESMISMATCHFloat         , new FormatInfo(Format.Bc6HUfloat       , 4 , 4 , 16, 4) },
            { (uint) GpuFormat.ETC2RGBAUnorm                , new FormatInfo(Format.Etc2RgbaUnorm    , 4 , 4 , 16, 4) },
            { (uint) GpuFormat.R32B24G8Snorm                , new FormatInfo(Format.Etc2RgbaSrgb     , 4 , 4 , 16, 4) },
            { (uint) GpuFormat.ASTC2D4X4Unorm               , new FormatInfo(Format.Astc4x4Unorm     , 4 , 4 , 16, 4) },
            { (uint) GpuFormat.ASTC2D5X4Unorm               , new FormatInfo(Format.Astc5x4Unorm     , 5 , 4 , 16, 4) },
            { (uint) GpuFormat.ASTC2D5X5Unorm               , new FormatInfo(Format.Astc5x5Unorm     , 5 , 5 , 16, 4) },
            { (uint) GpuFormat.ASTC2D6X5Unorm               , new FormatInfo(Format.Astc6x5Unorm     , 6 , 5 , 16, 4) },
            { (uint) GpuFormat.ASTC2D6X6Unorm               , new FormatInfo(Format.Astc6x6Unorm     , 6 , 6 , 16, 4) },
            { (uint) GpuFormat.ASTC2D8X5Unorm               , new FormatInfo(Format.Astc8x5Unorm     , 8 , 5 , 16, 4) },
            { (uint) GpuFormat.ASTC2D8X6Unorm               , new FormatInfo(Format.Astc8x6Unorm     , 8 , 6 , 16, 4) },
            { (uint) GpuFormat.ASTC2D8X8Unorm               , new FormatInfo(Format.Astc8x8Unorm     , 8 , 8 , 16, 4) },
            { (uint) GpuFormat.ASTC2D10X5Unorm              , new FormatInfo(Format.Astc10x5Unorm    , 10, 5 , 16, 4) },
            { (uint) GpuFormat.ASTC2D10X6Unorm              , new FormatInfo(Format.Astc10x6Unorm    , 10, 6 , 16, 4) },
            { (uint) GpuFormat.ASTC2D10X8Unorm              , new FormatInfo(Format.Astc10x8Unorm    , 10, 8 , 16, 4) },
            { (uint) GpuFormat.ASTC2D10X10Unorm             , new FormatInfo(Format.Astc10x10Unorm   , 10, 10, 16, 4) },
            { (uint) GpuFormat.ASTC2D12X10Unorm             , new FormatInfo(Format.Astc12x10Unorm   , 12, 10, 16, 4) },
            { (uint) GpuFormat.ASTC2D12X12Unorm             , new FormatInfo(Format.Astc12x12Unorm   , 12, 12, 16, 4) },
            { (uint) GpuFormat.E5B9G9R9SHAREDEXPSnorm_First , new FormatInfo(Format.Astc4x4Srgb      , 4 , 4 , 16, 4) },
            { (uint) GpuFormat.DXN2Snorm_First              , new FormatInfo(Format.Astc5x4Srgb      , 5 , 4 , 16, 4) },
            // { (uint) GpuFormat.E5B9G9R9SHAREDEXPSnorm_Second, new FormatInfo(Format.Astc5x5Srgb      , 5 , 5 , 16, 4) },
            // { (uint) GpuFormat.DXN2Snorm_Second             , new FormatInfo(Format.Astc6x5Srgb      , 6 , 5 , 16, 4) },
            { (uint) GpuFormat.BF10GF11RF11Snorm            , new FormatInfo(Format.Astc6x6Srgb      , 6 , 6 , 16, 4) },
            { (uint) GpuFormat.X8Z24Snorm_First             , new FormatInfo(Format.Astc8x5Srgb      , 8 , 5 , 16, 4) },
            { (uint) GpuFormat.Z24S8Snorm_First             , new FormatInfo(Format.Astc8x6Srgb      , 8 , 6 , 16, 4) },
            { (uint) GpuFormat.G8B8G8R8Snorm_First          , new FormatInfo(Format.Astc8x8Srgb      , 8 , 8 , 16, 4) },
            { (uint) GpuFormat.S8Z24Snorm_First             , new FormatInfo(Format.Astc10x5Srgb     , 10, 5 , 16, 4) },
            // { (uint) GpuFormat.S8Z24Snorm_Second            , new FormatInfo(Format.Astc10x6Srgb     , 10, 6 , 16, 4) },
            // { (uint) GpuFormat.Z24S8Snorm_Second            , new FormatInfo(Format.Astc10x8Srgb     , 10, 8 , 16, 4) },
            // { (uint) GpuFormat.G8B8G8R8Snorm_Second         , new FormatInfo(Format.Astc10x10Srgb    , 10, 10, 16, 4) },
            // { (uint) GpuFormat.X8Z24Snorm_Second            , new FormatInfo(Format.Astc12x10Srgb    , 12, 10, 16, 4) },
            { (uint) GpuFormat.B8G8R8G8Snorm                , new FormatInfo(Format.Astc12x12Srgb    , 12, 12, 16, 4) },
            { (uint) GpuFormat.A5B5G5R1Unorm                , new FormatInfo(Format.A1B5G5R5Unorm    , 1 , 1 , 2 , 4) }
        };

        private static Dictionary<ulong, Format> _attribFormats = new Dictionary<ulong, Format>()
        {
            { 0x13a00000, Format.R8Unorm             },
            { 0x0ba00000, Format.R8Snorm             },
            { 0x23a00000, Format.R8Uint              },
            { 0x1ba00000, Format.R8Sint              },
            { 0x3b600000, Format.R16Float            },
            { 0x13600000, Format.R16Unorm            },
            { 0x0b600000, Format.R16Snorm            },
            { 0x23600000, Format.R16Uint             },
            { 0x1b600000, Format.R16Sint             },
            { 0x3a400000, Format.R32Float            },
            { 0x22400000, Format.R32Uint             },
            { 0x1a400000, Format.R32Sint             },
            { 0x13000000, Format.R8G8Unorm           },
            { 0x0b000000, Format.R8G8Snorm           },
            { 0x23000000, Format.R8G8Uint            },
            { 0x1b000000, Format.R8G8Sint            },
            { 0x39e00000, Format.R16G16Float         },
            { 0x11e00000, Format.R16G16Unorm         },
            { 0x09e00000, Format.R16G16Snorm         },
            { 0x21e00000, Format.R16G16Uint          },
            { 0x19e00000, Format.R16G16Sint          },
            { 0x38800000, Format.R32G32Float         },
            { 0x20800000, Format.R32G32Uint          },
            { 0x18800000, Format.R32G32Sint          },
            { 0x12600000, Format.R8G8B8Unorm         },
            { 0x0a600000, Format.R8G8B8Snorm         },
            { 0x22600000, Format.R8G8B8Uint          },
            { 0x1a600000, Format.R8G8B8Sint          },
            { 0x38a00000, Format.R16G16B16Float      },
            { 0x10a00000, Format.R16G16B16Unorm      },
            { 0x08a00000, Format.R16G16B16Snorm      },
            { 0x20a00000, Format.R16G16B16Uint       },
            { 0x18a00000, Format.R16G16B16Sint       },
            { 0x38400000, Format.R32G32B32Float      },
            { 0x20400000, Format.R32G32B32Uint       },
            { 0x18400000, Format.R32G32B32Sint       },
            { 0x11400000, Format.R8G8B8A8Unorm       },
            { 0x09400000, Format.R8G8B8A8Snorm       },
            { 0x21400000, Format.R8G8B8A8Uint        },
            { 0x19400000, Format.R8G8B8A8Sint        },
            { 0x38600000, Format.R16G16B16A16Float   },
            { 0x10600000, Format.R16G16B16A16Unorm   },
            { 0x08600000, Format.R16G16B16A16Snorm   },
            { 0x20600000, Format.R16G16B16A16Uint    },
            { 0x18600000, Format.R16G16B16A16Sint    },
            { 0x38200000, Format.R32G32B32A32Float   },
            { 0x20200000, Format.R32G32B32A32Uint    },
            { 0x18200000, Format.R32G32B32A32Sint    },
            { 0x16000000, Format.R10G10B10A2Unorm    },
            { 0x26000000, Format.R10G10B10A2Uint     },
            { 0x3e200000, Format.R11G11B10Float      },
            { 0x2ba00000, Format.R8Uscaled           },
            { 0x33a00000, Format.R8Sscaled           },
            { 0x2b600000, Format.R16Uscaled          },
            { 0x33600000, Format.R16Sscaled          },
            { 0x2a400000, Format.R32Uscaled          },
            { 0x32400000, Format.R32Sscaled          },
            { 0x2b000000, Format.R8G8Uscaled         },
            { 0x33000000, Format.R8G8Sscaled         },
            { 0x29e00000, Format.R16G16Uscaled       },
            { 0x31e00000, Format.R16G16Sscaled       },
            { 0x28800000, Format.R32G32Uscaled       },
            { 0x30800000, Format.R32G32Sscaled       },
            { 0x2a600000, Format.R8G8B8Uscaled       },
            { 0x32600000, Format.R8G8B8Sscaled       },
            { 0x28a00000, Format.R16G16B16Uscaled    },
            { 0x30a00000, Format.R16G16B16Sscaled    },
            { 0x28400000, Format.R32G32B32Uscaled    },
            { 0x30400000, Format.R32G32B32Sscaled    },
            { 0x29400000, Format.R8G8B8A8Uscaled     },
            { 0x31400000, Format.R8G8B8A8Sscaled     },
            { 0x28600000, Format.R16G16B16A16Uscaled },
            { 0x30600000, Format.R16G16B16A16Sscaled },
            { 0x28200000, Format.R32G32B32A32Uscaled },
            { 0x30200000, Format.R32G32B32A32Sscaled },
            { 0x0e000000, Format.R10G10B10A2Snorm    },
            { 0x1e000000, Format.R10G10B10A2Sint     },
            { 0x2e000000, Format.R10G10B10A2Uscaled  },
            { 0x36000000, Format.R10G10B10A2Sscaled  }
        };

        /// <summary>
        /// Try getting the texture format from an encoded format integer from the Maxwell texture descriptor.
        /// </summary>
        /// <param name="encoded">The encoded format integer from the texture descriptor</param>
        /// <param name="isSrgb">Indicates if the format is a sRGB format</param>
        /// <param name="format">The output texture format</param>
        /// <returns>True if the format is valid, false otherwise</returns>
        public static bool TryGetTextureFormat(uint encoded, bool isSrgb, out FormatInfo format)
        {
            encoded |= (isSrgb ? 1u << 19 : 0u);

            return _textureFormats.TryGetValue(encoded, out format);
        }

        /// <summary>
        /// Try getting the vertex attribute format from an encoded format integer from Maxwell attribute registers.
        /// </summary>
        /// <param name="encoded">The encoded format integer from the attribute registers</param>
        /// <param name="format">The output vertex attribute format</param>
        /// <returns>True if the format is valid, false otherwise</returns>
        public static bool TryGetAttribFormat(uint encoded, out Format format)
        {
            return _attribFormats.TryGetValue(encoded, out format);
        }
    }
}