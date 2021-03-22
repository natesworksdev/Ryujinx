using Ryujinx.Graphics.OpenGL.Helper;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.OpenGL
{
    public interface IOpenGLContext : IDisposable
    {
        void MakeCurrent();

        // TODO: Support more APIs per platform.
        public static bool HasContext()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return WGLHelper.GetCurrentContext() != IntPtr.Zero;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GLXHelper.GetCurrentContext() != IntPtr.Zero;
            }
            else
            {
                return false;
            }
        }
    }
}
