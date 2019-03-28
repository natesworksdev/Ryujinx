using System;

namespace Ryujinx.Graphics.Shader.IntermediateRepresentation
{
    [Flags]
    enum TextureType
    {
        Texture1D,
        Texture2D,
        Texture3D,
        TextureCube,

        Mask = 0xff,

        Array        = 1 << 8,
        DepthCompare = 1 << 9,
        LodBias      = 1 << 10,
        LodLevel     = 1 << 11,
        Offset       = 1 << 12,

        LodLevelDepthCompare = LodLevel | DepthCompare
    }
}