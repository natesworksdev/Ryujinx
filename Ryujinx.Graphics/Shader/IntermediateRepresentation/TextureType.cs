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

        Array  = 1 << 8,
        Shadow = 1 << 9
    }
}