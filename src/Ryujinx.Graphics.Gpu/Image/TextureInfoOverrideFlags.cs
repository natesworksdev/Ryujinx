using System;

namespace Ryujinx.Graphics.Gpu.Image
{
    /// <summary>
    /// Flags controlling which parameters of the texture should be overriden.
    /// </summary>
    [Flags]
    enum TextureInfoOverrideFlags
    {
        /// <summary>
        /// Nothing should be overriden.
        /// </summary>
        None = 0,

        /// <summary>
        /// The texture size (width, height, depth and levels) should be overriden.
        /// </summary>
        OverrideSize = 1 << 0,

        /// <summary>
        /// The texture format should be overriden.
        /// </summary>
        OverrideFormat = 1 << 1
    }
}
