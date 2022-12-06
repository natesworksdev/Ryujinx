using System;

namespace Ryujinx.Graphics.Texture.Encoders
{
    [Flags]
    enum EncodeMode
    {
        Fast,
        Exhaustive,
        ModeMask = 0xff,
        Multithreaded = 1 << 8
    }
}
