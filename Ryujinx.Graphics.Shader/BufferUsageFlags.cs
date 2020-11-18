using System;

namespace Ryujinx.Graphics.Shader
{
    /// <summary>
    /// Flags that indicate how a buffer will be used in a shader.
    /// </summary>
    [Flags]
    public enum BufferUsageFlags
    {
        None = 0,

        // Buffer is written to.
        Write = 1 << 0
    }
}
