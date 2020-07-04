using System;

namespace Ryujinx.Graphics.Shader
{
    [Flags]
    public enum TextureUsageFlags
    {
        None = 0,

        // Integer sampled textures must be noted for resolution scaling.
        ResScaleUnsupported = 1 << 0
    }
}
