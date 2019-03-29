using System;

namespace Ryujinx.Graphics.Shader.IntermediateRepresentation
{
    [Flags]
    enum TextureFlags
    {
        None     = 0,
        Gather   = 1 << 0,
        LodBias  = 1 << 1,
        LodLevel = 1 << 2,
        Offset   = 1 << 3,
        Offsets  = 1 << 4
    }
}